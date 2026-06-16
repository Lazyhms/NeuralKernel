using NeuralKernel.StoreDefinition;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using OllamaSharp.Models;
using System.Text;
using System.Text.Json;

namespace NeuralKernel.Apis;

public static class RagChat
{
    private const string RAG_PROMPT = @"你是专业的企业制度问答助手。你的回答必须严格基于当前对话中提供的【事实依据】部分。【重要规则】1. 你的所有思考过程、推理、分析、内部逻辑，必须*全部使用中文**，禁止使用英文思考。请用中文思考，再用中文回答。2. 仅使用【事实依据】中的信息回答问题。若存在明显的OCR识别错误，请直接根据上下文和常识进行合理纠正，无需指出或列举具体错误。无法确定正确内容的部分，请忽略，仅使用可读的正常文本。3. 如果纠正并忽略错误后，仍没有足够信息回答用户问题，必须直接回复：""未找到相关信息""。4. 回答时，内容需全面、条理清晰，使用标准中文。无需说明信息来源，直接给出答案内容。";

    private const int MAX_HISTORY_MESSAGES = 10;
    private const string CACHE_KEY_PREFIX = "RAGCHATHISTORY_";
    public const string X_CHAT_SESSION_ID = "X-Rag-Chat-Session-Id";

    public static RouteGroupBuilder MapRagChat(this RouteGroupBuilder builder)
    {
        builder.MapGet("completion", async (
            Kernel kernel,
            HttpContext httpContext,
            IMemoryCache memoryCache,
            QdrantVectorStore vectorStore,
            IChatCompletionService chatCompletion,
            IOptionsSnapshot<ModelOptions> optionsSnapshot,
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            string index, string question, string? sessionId = null, CancellationToken cancellationToken = default) =>
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
            }

            httpContext.Response.Headers.Append(X_CHAT_SESSION_ID, sessionId);

            var cacheKey = $"{CACHE_KEY_PREFIX}{sessionId}";
            var chatHistory = await memoryCache.GetOrCreateAsync(cacheKey, async factory =>
            {
                factory.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                return new ChatHistory(RAG_PROMPT)!;
            });

            var searchResults = new StringBuilder();
            var questionVector = await embeddingGenerator.GenerateVectorAsync(question, new EmbeddingGenerationOptions
            {
                ModelId = optionsSnapshot.Value.Embedding,
            }, cancellationToken: cancellationToken);
            var collection = vectorStore.GetCollection<Guid, PolicyDocument>(index);
            await foreach (var item in collection.SearchAsync(questionVector, 10, new VectorSearchOptions<PolicyDocument>
            {
                IncludeVectors = false,
                ScoreThreshold = 0.5D,
                VectorProperty = v => v.Vectors,
            }, cancellationToken))
            {
                var payload = JsonSerializer.Deserialize<Payload>(item.Record.Payload)!;
                searchResults.AppendLine($"==== [来源文件：{payload.File} | 相关性得分：{item.Score:F2}] ====");
                searchResults.AppendLine(payload.Text);
                searchResults.AppendLine();
            }

            if (searchResults.Length == 0)
            {
                return Results.Text("未找到相关信息", "text/event-stream; charset=utf-8");
            }

            var userMessage = $@"【事实依据】{searchResults.ToString().Trim()}
【用户问题】{question}";
            chatHistory!.AddUserMessage(userMessage);

            return Results.Stream(async stream =>
            {
                var fullAnswer = new StringBuilder();

                await foreach (var item in chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory, new OllamaPromptExecutionSettings
                {
                    Temperature = 0.1F,
                    ModelId = optionsSnapshot.Value.Chat,
                }.AddOllamaOption(OllamaOption.Think, true), kernel))
                {
                    if (string.IsNullOrWhiteSpace(item.Content)) { continue; }

                    fullAnswer.Append(item.Content);

                    await stream.WriteDataAsync(item.Content);
                }

                await stream.WriteNewLineAsync();
                await stream.WriteDoneAsync();

                chatHistory.AddAssistantMessage(fullAnswer.ToString());
                TrimChatHistory(chatHistory);
                memoryCache.Set(cacheKey, chatHistory, TimeSpan.FromMinutes(30));

            }, "text/event-stream; charset=utf-8");
        }).WithSummary("知识库问答");

        return builder;
    }

    private static void TrimChatHistory(ChatHistory chat)
    {
        if (chat.Count > MAX_HISTORY_MESSAGES + 1)
        {
            var messagesToRemove = chat.Count - (MAX_HISTORY_MESSAGES + 1);
            chat.RemoveRange(1, messagesToRemove);
        }
    }
}