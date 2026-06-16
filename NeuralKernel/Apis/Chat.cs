using NeuralKernel.Core.DataFormats;
using NeuralKernel.Core.Pipeline;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;
using System.Text;
using UglyToad.PdfPig;

namespace NeuralKernel.Apis;

public record ChatRequest(string Question, IFormFileCollection Files)
{
    public string? SessionId { get; set; }
}

public static class Chat
{
    private const string SYSTEM_PROMPT = @"����һ��רҵ�����ܶԻ����֣��ش���������ࡢ׼ȷ��ʹ�ñ�׼���Ľ�����
����Ҫ����
1. �������˼�����̡�������������ڲ��߼�������**ȫ��ʹ������**����ֹʹ��Ӣ��˼������������˼�����������Ļش�
2. ����пɵ��õĹ������ȵ��ù��ߣ������ѹ��߷��صĽ��Ϊ׼��
3. ����û��ϴ����ļ��������Ȼ����ļ����ݻش𣬲�������Ϣ��
4. ���û���ϴ��ļ���������Լ���֪ʶ�����ش����⡣
5. ����ļ����ݲ����Իش��û����⣬����ʵ��֪��";

    public const string X_CHAT_SESSION_ID = "X-Chat-Session-Id";

    private const string CACHE_KEY_PREFIX = "PURECHATHISTORY_";

    private const int MAX_HISTORY_COUNT = 15;

    public static RouteGroupBuilder MapChat(this RouteGroupBuilder builder)
    {
        builder.MapPost("completion", async (
            Kernel kernel,
            HttpContext httpContext,
            IMemoryCache memoryCache,
            [FromForm] ChatRequest request,
            IMimeTypeDetection mimeTypeDetection,
            IChatCompletionService chatCompletion,
            IEnumerable<IContentDecoder> contentDecoder,
            IOptionsSnapshot<ModelOptions> optionsSnapshot) =>
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return Results.BadRequest("�������ݲ���Ϊ��");
            }

            httpContext.Response.Headers.Append(X_CHAT_SESSION_ID, request.SessionId);

            var cacheKey = $"{CACHE_KEY_PREFIX}{request.SessionId}";
            var chatHistory = await memoryCache.GetOrCreateAsync(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                return Task.FromResult(new ChatHistory(SYSTEM_PROMPT))!;
            })!;

            var fileContentBuilder = new StringBuilder();
            var contentItemCollection = new ChatMessageContentItemCollection();
            foreach (var file in request.Files)
            {
                if (file == null || file.Length == 0) { continue; }

                if (file.ContentType.StartsWith("image"))
                {
                    using var imageStream = file.OpenReadStream();
                    Memory<byte> memory = new byte[imageStream.Length];
                    await imageStream.ReadExactlyAsync(memory);

                    contentItemCollection.Add(new ImageContent(memory, file.ContentType));

                    continue;
                }

                if (file.ContentType == MimeTypes.Pdf)
                {
                    using var pdfStream = file.OpenReadStream();
                    var p = PdfDocument.Open(pdfStream);
                    foreach (var item in p.GetPages())
                    {
                        foreach (var item1 in item.GetImages())
                        {
                            if (!item1.TryGetPng(out var bytes))
                            {
                                if (!item1.TryGetBytesAsMemory(out var m))
                                {
                                    m = item1.RawMemory;
                                }

                                bytes = m.ToArray();
                            }

                            using var dd = Mat.FromImageData(bytes, ImreadModes.Grayscale);

                            using Mat denoised = new();
                            Cv2.GaussianBlur(dd, denoised, new Size(3, 3), 0);

                            using PaddleOcrAll ocr = new(LocalFullModels.ChineseV4, PaddleDevice.Gpu())
                            {
                                AllowRotateDetection = true,
                                Enable180Classification = true,
                            };
                            var r = ocr.Run(denoised);
                            fileContentBuilder.AppendLine(r.Text);
                        }
                    }
                }

                if (mimeTypeDetection.TryGetFileType(file.ContentType, out var mimeType) && !string.IsNullOrWhiteSpace(mimeType))
                {
                    var decoder = contentDecoder.LastOrDefault(o => o.SupportsMimeType(mimeType));
                    if (decoder == null) { continue; }

                    using var stream = file.OpenReadStream();
                    var content = await decoder.DecodeAsync(stream);
                    foreach (var section in content.Sections)
                    {
                        var sectionContent = section.Content.Trim();
                        if (!string.IsNullOrEmpty(sectionContent))
                            fileContentBuilder.AppendLine(sectionContent);
                    }
                }
            }

            var userMessageBuilder = new StringBuilder(request.Question);
            var fileContent = fileContentBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(fileContent))
            {
                userMessageBuilder.AppendLine("\n\n���ϴ��ļ����ݡ�");
                userMessageBuilder.AppendLine(fileContent);
            }

            var finalUserMessage = userMessageBuilder.ToString();
            contentItemCollection.Add(new TextContent(finalUserMessage));
            if (contentItemCollection.Count > 0)
            {
                chatHistory!.AddUserMessage(contentItemCollection);
            }

            //var mcpClient = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
            //{
            //    Endpoint = new Uri("http://localhost:5109/mcp")
            //}));
            //var mcpClientTools = await mcpClient.ListToolsAsync();

            //kernel.Plugins.AddFromType<OAPlugin>("OA");

            //kernel.Plugins.AddFromFunctions("TEST", mcpClientTools.Select(s => s.AsKernelFunction()));

            return Results.Stream(async stream =>
            {
                var fullAnswer = new StringBuilder();

                await foreach (var item in chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory!, new OllamaPromptExecutionSettings
                {
                    Temperature = 0.6f,
                    ModelId = optionsSnapshot.Value.Chat,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                }.AddOllamaOption(OllamaOption.Think, true), kernel))
                {
                    if (item.InnerContent is ChatResponseStream responseStream)
                    {
                        if (!string.IsNullOrWhiteSpace(responseStream.Message.Thinking))
                        {
                            Console.Write(responseStream.Message.Thinking);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(item.Content)) { continue; }

                    fullAnswer.Append(item.Content);

                    Console.Write(item.Content);
                    await stream.WriteDataAsync(item.Content);
                }

                chatHistory!.AddAssistantMessage(fullAnswer.ToString());

                TrimChatHistory(chatHistory);

                memoryCache.Set(cacheKey, chatHistory, TimeSpan.FromMinutes(30));

                await stream.WriteNewLineAsync();
                await stream.WriteDoneAsync();

            }, contentType: "text/event-stream; charset=utf-8");
        }).WithSummary("�ʴ�").DisableAntiforgery();

        return builder;
    }

    private static void TrimChatHistory(ChatHistory chatHistory)
    {
        if (chatHistory.Count > MAX_HISTORY_COUNT + 1)
        {
            var removeCount = chatHistory.Count - (MAX_HISTORY_COUNT + 1);
            chatHistory.RemoveRange(1, removeCount);
        }
    }
}