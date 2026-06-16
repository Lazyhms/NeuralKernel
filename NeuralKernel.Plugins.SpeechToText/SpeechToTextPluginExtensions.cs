using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel;
using Whisper.net;

namespace NeuralKernel.Plugins.SpeechToText;

public static class SpeechToTextPluginExtensions
{
    public static IKernelBuilder AddSpeechToTextPlugin(this IKernelBuilder builder, string modelId = "")
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentNullException(nameof(modelId), "模型文件不能为空");
        }

        if (".bin" != Path.GetExtension(modelId))
        {
            throw new NotSupportedException("模型文件不支持，请使用.bin类型");
        }

        var modelPath = Path.Combine(AppContext.BaseDirectory, "Models", modelId);

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("模型文件不存在，请确保模型文件已正确放置", modelId);
        }

        builder.Services.TryAddSingleton<WhisperFactory>(_ => WhisperFactory.FromPath(modelPath));

        builder.Plugins.AddFromType<SpeechToTextPlugin>();

        return builder;
    }
}