using NeuralKernel.Core;
using NeuralKernel.Plugins.SpeechToText;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using OllamaSharp.Models;
using System.Text;

namespace NeuralKernel.Apis;

public record SpeechToTextAgentRequest(IFormFileCollection Files)
{
    public string? SessionId { get; set; }
    public string? Question { get; set; }
}

public static class SpeechToTextAgent
{
    private const string SYSTEM_PROMPT = @"你是一个专业的音频处理智能助手，擅长处理语音转文字任务。【重要规则】1. 你的所有思考过程、推理、分析、内部逻辑，必须*全部使用中文**，禁止使用英文思考。2. 用户上传音频文件时，优先使用语音转文字工具进行转录。3. 如果用户同时上传了音频文件和问题，先转录音频内容，然后根据转录结果回答问题。4. 如果没有问题，只返回转录结果即可。5. 如果没有上传音频文件，直接回答用户问题。";

    public const string X_CHAT_SESSION_ID = "X-Audio-Session-Id";

    private const string CACHE_KEY_PREFIX = "AUDIOAGENTHISTORY_";

    private const int MAX_HISTORY_COUNT = 10;

    public static RouteGroupBuilder MapSpeechToText(this RouteGroupBuilder builder)
    {
        builder.MapPost("transcribe", async (
            Kernel kernel,
            HttpContext httpContext,
            IMemoryCache memoryCache,
            [FromForm] SpeechToTextAgentRequest request,
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

            var transcriptBuilder = new StringBuilder();

            foreach (var file in request.Files)
            {
                if (file == null || file.Length == 0) continue;

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var supportedFormats = SpeechToTextPlugin.SupportFormats();

                if (!supportedFormats.Contains(extension)) continue;

                using var originalStream = file.OpenReadStream();
                var serializableStream = new SerializableStream(originalStream);

                var plugin = kernel.Plugins["SpeechToTextPlugin"];
                var result = await kernel.InvokeAsync(plugin["TranscribeAudioStream"], new()
                {
                    ["stream"] = serializableStream,
                    ["language"] = "auto"
                });

                var transcript = result.GetValue<string>() ?? string.Empty;

                transcriptBuilder.AppendLine($"【音频文件 {file.FileName} 转录结果】");
                transcriptBuilder.AppendLine(transcript);
                transcriptBuilder.AppendLine();
            }

            var userMessageBuilder = new StringBuilder();

            if (transcriptBuilder.Length > 0)
            {
                userMessageBuilder.Append(transcriptBuilder);
            }

            if (!string.IsNullOrWhiteSpace(request.Question))
            {
                userMessageBuilder.AppendLine("【用户问题】");
                userMessageBuilder.AppendLine(request.Question);
            }

            var finalUserMessage = userMessageBuilder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(finalUserMessage))
            {
                return Results.BadRequest("请上传音频文件或输入问题");
            }

            chatHistory!.AddUserMessage(finalUserMessage);

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
        }).WithSummary("音频智能助手").DisableAntiforgery();

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