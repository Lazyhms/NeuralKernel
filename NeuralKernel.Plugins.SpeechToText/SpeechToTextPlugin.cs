using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
using Whisper.net;

namespace NeuralKernel.Plugins.SpeechToText;

[Description("语音识别插件，提供语音转文字功能。支持音频文件和音频流")]
public sealed class SpeechToTextPlugin(WhisperFactory whisperFactory, ILogger<SpeechToTextPlugin> logger)
{
    private static readonly IReadOnlyList<string> _supportedAudioFormats = [".wav", ".mp3", ".m4a", ".ogg", ".flac"];

    [KernelFunction, Description("获取支持的音频格式列表")]
    public static IReadOnlyList<string> SupportFormats() => _supportedAudioFormats;

    [KernelFunction, Description("将音频流转写为文本内容，支持实时和流式处理")]
    public async Task<string> TranscribeAudioStreamAsync(
        [Description("音频流数据")] Stream stream,
        [Description("提示词，帮助模型理解上下文")] string? prompt = null,
        [Description("语言代码，默认为auto自动检测，支持zh(中文), en(英文), ja(日文), ko(韩文)等")] string language = "auto",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException("音频流必须支持读取操作", nameof(stream));
        }

        var builder = whisperFactory.CreateBuilder()
            .WithLanguage(language);

        if (!string.IsNullOrEmpty(prompt))
        {
            builder = builder.WithPrompt(prompt);
        }

        if (language == "auto")
        {
            builder = builder.WithLanguageDetection();
        }

        using var processor = builder.WithTemperature(0.0F).WithTemperatureInc(0.0F).Build();

        var fullText = new StringBuilder();

        await foreach (var segment in processor.ProcessAsync(stream, cancellationToken))
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("[{Start} --> {End}] {Text}", segment.Start, segment.End, segment.Text);
            }

            fullText.Append(segment.Text);
        }

        return fullText.ToString();
    }
}