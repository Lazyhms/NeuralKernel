using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
using Whisper.net;

namespace NeuralKernel.Plugins.SpeechToText;

[KernelPlugin]
[Description("��Ƶ���������ṩ����ת���֡���Ƶ�ļ�����ȹ���")]
public sealed class SpeechToTextPlugin(WhisperFactory whisperFactory, ILogger<SpeechToTextPlugin> logger)
{
    private static readonly IReadOnlyList<string> _supportedAudioFormats = [".wav", ".mp3", ".m4a", ".ogg", ".flac"];

    [KernelFunction, Description("��ȡ֧�ֵ���Ƶ��ʽ�б�")]
    public static IReadOnlyList<string> SupportFormats() => _supportedAudioFormats;

    [KernelFunction, Description("����Ƶ��תдΪ�ı����ݣ�֧�����ĺͶ�������")]
    public async Task<string> TranscribeAudioStreamAsync(
        [Description("��Ƶ������")] Stream stream,
        [Description("��ʾ�ʣ�����ģ�����������")] string? prompt = null,
        [Description("���Դ��룬Ĭ��Ϊauto�Զ���⣬֧��zh(����), en(Ӣ��), ja(����), ko(����)��")] string language = "auto",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException("��Ƶ������֧�ֶ�ȡ������", nameof(stream));
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