using NeuralKernel.Plugins.Document;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using OllamaSharp.Models;
using System.Text;

namespace NeuralKernel.Apis;

public record DocumentAgentRequest(IFormFileCollection Files)
{
    public string? SessionId { get; set; }
    public string? Question { get; set; }
}

public static class DocumentAgent
{
    private const string SYSTEM_PROMPT = @"你是一个专业的文档处理智能助手，擅长处理各种文档内容分析任务。【重要规则】1. 你的所有思考过程、推理、分析、内部逻辑，必须*全部使用中文**，禁止使用英文思考。2. 用户上传文档文件时，优先使用文档读取工具进行内容提取。3. 如果用户同时上传了文档文件和问题，先提取文档内容，然后根据内容回答问题。4. 如果没有问题，只返回文档提取结果即可。5. 如果没有上传文档文件，直接回答用户问题。6. 回答时请保持中文，简洁明了。";

    public const string X_CHAT_SESSION_ID = "X-Document-Session-Id";

    private const string CACHE_KEY_PREFIX = "DOCUMENTAGENTHISTORY_";

    private const int MAX_HISTORY_COUNT = 10;

    public static RouteGroupBuilder MapDocumentAgent(this RouteGroupBuilder builder)
    {
        builder.MapPost("analyze", async (
            Kernel kernel,
            HttpContext httpContext,
            IMemoryCache memoryCache,
            [FromForm] DocumentAgentRequest request,
            IChatCompletionService chatCompletion,
            IOptionsSnapshot<ModelOptions> optionsSnapshot) =>
        {
            httpContext.Response.Headers.Append(X_CHAT_SESSION_ID, request.SessionId ?? Guid.NewGuid().ToString());

            var cacheKey = $"{CACHE_KEY_PREFIX}{request.SessionId}";
            var chatHistory = await memoryCache.GetOrCreateAsync(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                return Task.FromResult(new ChatHistory(SYSTEM_PROMPT))!;
            })!;

            var documentContentBuilder = new StringBuilder();

            foreach (var file in request.Files)
            {
                if (file == null || file.Length == 0) continue;

                var mimeType = file.ContentType;
                if (string.IsNullOrWhiteSpace(mimeType))
                {
                    mimeType = GetMimeTypeFromFileName(file.FileName);
                }

                using var stream = file.OpenReadStream();

                try
                {
                    var plugin = kernel.Plugins["DocumentPlugin"];
                    var result = await kernel.InvokeAsync(plugin["ReadDocumentFromStream"], new()
                    {
                        ["stream"] = stream,
                        ["mimeType"] = mimeType
                    });

                    var content = result.GetValue<string>() ?? string.Empty;

                    documentContentBuilder.AppendLine($"【文档文件 {file.FileName} 内容】");
                    documentContentBuilder.AppendLine(content);
                    documentContentBuilder.AppendLine();
                }
                catch (Exception ex)
                {
                    documentContentBuilder.AppendLine($"【文档文件 {file.FileName} 处理失败】");
                    documentContentBuilder.AppendLine($"错误: {ex.Message}");
                    documentContentBuilder.AppendLine();
                }
            }

            var userMessageBuilder = new StringBuilder();

            if (documentContentBuilder.Length > 0)
            {
                userMessageBuilder.Append(documentContentBuilder);
            }

            if (!string.IsNullOrWhiteSpace(request.Question))
            {
                userMessageBuilder.AppendLine("【用户问题】");
                userMessageBuilder.AppendLine(request.Question);
            }

            var finalUserMessage = userMessageBuilder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(finalUserMessage))
            {
                return Results.BadRequest("请上传文档文件或输入问题");
            }

            chatHistory.AddUserMessage(finalUserMessage);

            return Results.Stream(async stream =>
            {
                var fullAnswer = new StringBuilder();

                await foreach (var item in chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory, new OllamaPromptExecutionSettings
                {
                    Temperature = 0.7f,
                    ModelId = optionsSnapshot.Value.Chat,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                }.AddOllamaOption(OllamaOption.Think, true), kernel))
                {
                    if (string.IsNullOrWhiteSpace(item.Content)) continue;

                    fullAnswer.Append(item.Content);
                    await stream.WriteDataAsync(item.Content);
                }

                chatHistory.AddAssistantMessage(fullAnswer.ToString());
                TrimChatHistory(chatHistory);
                memoryCache.Set(cacheKey, chatHistory, TimeSpan.FromMinutes(30));

                await stream.WriteNewLineAsync();
                await stream.WriteDoneAsync();

            }, contentType: "text/event-stream; charset=utf-8");
        }).WithSummary("文档智能助手 - 分析文档并回答问题").DisableAntiforgery();

        builder.MapPost("extract", async (
            Kernel kernel,
            [FromForm] IFormFileCollection files) =>
        {
            var results = new List<object>();

            foreach (var file in files)
            {
                if (file == null || file.Length == 0) continue;

                try
                {
                    using var stream = file.OpenReadStream();
                    var plugin = kernel.Plugins["DocumentPlugin"];
                    var result = await kernel.InvokeAsync(plugin["ReadDocumentFromStream"], new()
                    {
                        ["fileStream"] = stream,
                        ["mimeType"] = file.ContentType
                    });

                    var content = result.GetValue<string>() ?? string.Empty;
                    results.Add(new { FileName = file.FileName, Success = true, Content = content });
                }
                catch (Exception ex)
                {
                    results.Add(new { FileName = file.FileName, Success = false, Message = ex.Message });
                }
            }

            return Results.Ok(results);
        }).WithSummary("仅提取文档内容").DisableAntiforgery();

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

    private static string GetMimeTypeFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".html" => "text/html",
            _ => "application/octet-stream"
        };
    }
}