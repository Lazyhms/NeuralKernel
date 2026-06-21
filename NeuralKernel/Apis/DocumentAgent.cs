using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
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
    private const string SYSTEM_PROMPT = @"你是一个多功能的智能助手，可以根据用户需求灵活处理以下场景：

## 支持的场景

### 1. 文档问答
- 解析用户上传的文档，提取关键信息
- 回答关于文档内容的问题
- 总结、归纳文档要点

### 2. 文档生成
根据用户需求生成各类文档，使用 Markdown 格式输出：
- **技术文档**：背景 → 目标 → 方案设计 → 落地步骤 → 风险评估
- **数据报表**：表格维度规划 → 指标层级设计
- **工作总结**：概述 → 主要工作 → 成果亮点 → 问题与改进 → 下一步计划
- **会议纪要**：会议信息 → 议题讨论 → 决议事项 → 待办事项

### 3. 内容创作
支持各种创意写作：
- **故事创作**：童话、寓言、科幻、校园等各类故事
- **试卷生成**：各类学科试卷，包含选择题、填空题、简答题等
- **诗歌散文**：古诗、现代诗、散文等
- **教学材料**：教案、课件内容、练习题等

### 4. 知识问答
回答各类知识性问题，涵盖学习、工作、生活等领域。

## 输出格式

**Markdown 语法规范**（如需生成文档）：
- `#` `##` `###`：一/二/三级标题
- `-` 或 `1.`：无序/有序列表
- `| - |`：规整表格
- `**加粗**`：重点内容
- `---`：模块分隔

**保存文档**：如用户需要保存文档，调用 `Document-Write` 工具（参数说明会自动提供）

## 输出原则
1. **灵活响应**：根据用户意图选择最合适的方式
2. **直接输出**：无需每次都保存文件，先展示内容让用户确认
3. **结构清晰**：使用 Markdown 格式组织内容
4. **专业准确**：确保内容准确、专业、完整";

    public const string X_CHAT_SESSION_ID = "X-Document-Session-Id";

    private const string CACHE_KEY_PREFIX = "DOCUMENTAGENTHISTORY_";

    private const int MAX_HISTORY_COUNT = 10;

    private const long MAX_FILE_SIZE = 20 * 1024 * 1024;

    public static RouteGroupBuilder MapDocumentAgent(this RouteGroupBuilder builder)
    {
        builder.MapPost("chat", async (
            Kernel kernel,
            HttpContext httpContext,
            IMemoryCache memoryCache,
            [FromForm] DocumentAgentRequest request,
            IChatCompletionService chatCompletion,
            IOptionsSnapshot<ModelOptions> optionsSnapshot,
            ILoggerFactory loggerFactory, CancellationToken cancellationToken = default) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(DocumentAgent));

            var userMessage = request.Question ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userMessage) && request.Files.Count == 0)
            {
                return Results.BadRequest("请上传文档文件或输入问题");
            }

            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
            httpContext.Response.Headers.Append(X_CHAT_SESSION_ID, sessionId);

            var cacheKey = $"{CACHE_KEY_PREFIX}{sessionId}";
            var chatHistory = await memoryCache.GetOrCreateAsync<ChatHistory>(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                return Task.FromResult(new ChatHistory(SYSTEM_PROMPT));
            })!;

            if (request.Files.Count > 0)
            {
                var documentContents = new StringBuilder();
                foreach (var file in request.Files)
                {
                    if (file.Length > MAX_FILE_SIZE)
                    {
                        logger.LogWarning("文件 {FileName} 超过大小限制 {MaxSize}MB", file.FileName, MAX_FILE_SIZE / 1024 / 1024);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(file.ContentType))
                    {
                        logger.LogWarning("文件 {FileName} 缺少 MIME 类型", file.FileName);
                        continue;
                    }

                    try
                    {
                        using var stream = file.OpenReadStream();
                        var result = await kernel.InvokeAsync("Document", "Read", new()
                        {
                            ["stream"] = stream,
                            ["mimeType"] = file.ContentType,
                        }, cancellationToken).ConfigureAwait(false);

                        if (result is null)
                        {
                            continue;
                        }

                        var content = result.GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            documentContents.AppendLine($"【文档: {file.FileName}】");
                            documentContents.AppendLine(content);
                            documentContents.AppendLine();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "读取文件 {FileName} 失败", file.FileName);
                    }
                }

                if (documentContents.Length > 0)
                {
                    chatHistory!.AddUserMessage($"以下是我上传的文档内容，请基于这些内容回答问题：\n\n{documentContents}");
                }
            }

            if (!string.IsNullOrWhiteSpace(userMessage))
            {
                chatHistory!.AddUserMessage(userMessage);
            }

            var fullAnswer = new StringBuilder();

            await foreach (var item in chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory!, new OllamaPromptExecutionSettings
            {
                Temperature = 0.7f,
                ModelId = optionsSnapshot.Value.Chat,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            }.AddOllamaOption(OllamaOption.Think, true), kernel, cancellationToken))
            {
                if (item.InnerContent is ChatResponseStream responseStream)
                {
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        if (!string.IsNullOrWhiteSpace(responseStream.Message.Thinking))
                        {
                            logger.LogInformation("Thinking:{Thinking}", responseStream.Message.Thinking);
                        }

                        if (responseStream.Message.ToolCalls?.Any() == true)
                        {
                            var toolCallNames = responseStream.Message.ToolCalls
                                .Where(s => s.Function != null)
                                .Select(s => s.Function!.Name);
                            logger.LogInformation("ToolCalls:{ToolCalls}", string.Join(",", toolCallNames));
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(item.Content))
                {
                    fullAnswer.Append(item.Content);
                }
            }

            chatHistory!.AddAssistantMessage(fullAnswer.ToString());
            TrimChatHistory(chatHistory!);
            memoryCache.Set(cacheKey, chatHistory, TimeSpan.FromMinutes(30));

            return Results.Text(fullAnswer.ToString(), "text/plain; charset=utf-8");
        }).WithSummary("文档智能助手 - 上传文档分析、问答、生成文档").DisableAntiforgery();

        builder.MapGet("download/{fileId}", async (
            string fileId,
            ITempFileStorage tempFileStorage,
            CancellationToken cancellationToken = default) =>
        {
            var (stream, info) = await tempFileStorage.GetAsync(fileId, cancellationToken).ConfigureAwait(false);

            if (stream is null || info is null)
            {
                return Results.NotFound(new { Message = "文件不存在或已过期" });
            }

            return Results.File(stream, info.MimeType, info.FileName);
        }).WithSummary("下载生成的文档文件");

        return builder;
    }

    private static void TrimChatHistory(ChatHistory chatHistory)
    {
        if (chatHistory.Count <= MAX_HISTORY_COUNT + 1)
        {
            return;
        }

        var removeCount = chatHistory.Count - (MAX_HISTORY_COUNT + 1);
        chatHistory.RemoveRange(1, removeCount);
    }
}
