using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using NeuralKernel.Plugins.Core.FileMime;
using NeuralKernel.Plugins.Document;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Text;

namespace NeuralKernel.Apis;

public record DocumentAgentRequest(IFormFileCollection Files)
{
    public string? SessionId { get; set; }
    public string? Question { get; set; }
}

public static class DocumentAgent
{
    private const string SYSTEM_PROMPT = @"你是一个专业的文档处理智能助手。【核心规则】1. 所有思考过程、推理、分析、内部逻辑必须全部使用中文。2. 用户上传文档时根据需求读取需要的文档内容。3. 如果用户同时上传了文档和问题，先读取相关文档内容，然后根据内容回答问题或生成文档。4. 如果没有问题，只返回文档提取结果即可。5. 如果没有上传文档，直接回答用户问题或生成文档。6. 回答时请保持中文，简洁明了。【文档生成格式要求】生成文档内容时，必须使用 Markdown 格式：使用 # 表示一级标题，## 表示二级标题，### 表示三级标题；使用 - 或 * 表示无序列表；使用 1. 2. 3. 表示有序列表；使用 **加粗文本** 和 *斜体文本* 表示文字样式；使用 | 分隔表格列；使用 ``` 包裹代码块。生成的文档内容将直接用于创建 Word 等格式的文档文件。";

    public const string X_CHAT_SESSION_ID = "X-Document-Session-Id";

    private const string CACHE_KEY_PREFIX = "DOCUMENTAGENTHISTORY_";

    private const int MAX_HISTORY_COUNT = 10;

    public static RouteGroupBuilder MapDocumentAgent(this RouteGroupBuilder builder)
    {
        builder.MapPost("chat", async (
            Kernel kernel,
            HttpContext httpContext,
            IMemoryCache memoryCache,
            [FromForm] DocumentAgentRequest request,
            IChatCompletionService chatCompletion,
            IOptionsSnapshot<ModelOptions> optionsSnapshot,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(DocumentAgent));

            var userMessage = request.Question ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userMessage) && request.Files.Count == 0)
            {
                return Results.BadRequest("请上传文档文件或输入问题");
            }

            httpContext.Response.Headers.Append(X_CHAT_SESSION_ID, request.SessionId ?? Guid.NewGuid().ToString());

            var output = new DocumentOutput();
            DocumentOutputContext.Current = output;

            try
            {
                if (request.Files.Count > 0)
                {
                    var fileInfos = new List<DocumentFileInfo>();
                    foreach (var file in request.Files)
                    {
                        if (file == null || file.Length == 0) continue;

                        using var originalStream = file.OpenReadStream();
                        using var memoryStream = new MemoryStream();
                        await originalStream.CopyToAsync(memoryStream);

                        fileInfos.Add(new DocumentFileInfo
                        {
                            Size = file.Length,
                            Name = file.FileName,
                            MimeType = file.ContentType,
                            Content = memoryStream.ToArray()
                        });
                    }
                    DocumentFileContext.Current = fileInfos;
                }

                var cacheKey = $"{CACHE_KEY_PREFIX}{request.SessionId}";
                var chatHistory = await memoryCache.GetOrCreateAsync(cacheKey, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                    return Task.FromResult(new ChatHistory(SYSTEM_PROMPT))!;
                })!;

                chatHistory!.AddUserMessage(userMessage);

                var fullAnswer = new StringBuilder();

                kernel.Plugins.Clear();
                kernel.Plugins.AddFromType<FileMimePlugin>();
                kernel.Plugins.Add(kernel.CreatePluginFromType<DocumentPlugin>());

                await foreach (var item in chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory, new OllamaPromptExecutionSettings
                {
                    Temperature = 0.7f,
                    ModelId = optionsSnapshot.Value.Chat,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                }.AddOllamaOption(OllamaOption.Think, true), kernel))
                {
                    if (item.InnerContent is ChatResponseStream responseStream)
                    {
                        if (!string.IsNullOrWhiteSpace(responseStream.Message.Thinking))
                        {
                            if (logger.IsEnabled(LogLevel.Information))
                            {
                                logger.LogInformation("Thinking:{Thinking}", responseStream.Message.Thinking);
                            }
                        }

                        if (responseStream.Message.ToolCalls != null && responseStream.Message.ToolCalls.Any())
                        {
                            if (logger.IsEnabled(LogLevel.Information))
                            {
                                logger.LogInformation("Thinking:{Thinking}", string.Join(",", responseStream.Message.ToolCalls.Select(s => s.Function.Name)));
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(item.Content))
                    {
                        fullAnswer.Append(item.Content);
                    }
                }

                if (output.HasOutput)
                {
                    chatHistory.AddAssistantMessage($"[已保存 {output.FileName}]");
                    TrimChatHistory(chatHistory);
                    memoryCache.Set(cacheKey, chatHistory, TimeSpan.FromMinutes(30));

                    return Results.File(output.Data, output.MimeType, output.FileName);
                }

                chatHistory.AddAssistantMessage(fullAnswer.ToString());
                TrimChatHistory(chatHistory);
                memoryCache.Set(cacheKey, chatHistory, TimeSpan.FromMinutes(30));

                return Results.Text(fullAnswer.ToString(), "text/plain; charset=utf-8");
            }
            finally
            {
                DocumentOutputContext.Current = null;
                DocumentFileContext.Current = null;
            }
        }).WithSummary("文档智能助手 - 上传文档分析、问答、生成文档").DisableAntiforgery();

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
