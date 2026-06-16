using NeuralKernel.Plugins.Core.FileIO;
using NeuralKernel.Plugins.Core.Http;
using NeuralKernel.Plugins.Core.Time;
using Microsoft.SemanticKernel;

namespace NeuralKernel.Plugins.Core;

public static class CorePluginExtensions
{
    public static IKernelBuilder AddCorePlugin(this IKernelBuilder builder)
    {
        builder.Plugins.AddFromType<HttpPlugin>();
        builder.Plugins.AddFromType<TimePlugin>();
        builder.Plugins.AddFromType<FileIOPlugin>();

        return builder;
    }
}
