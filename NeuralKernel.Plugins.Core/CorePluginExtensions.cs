using Microsoft.SemanticKernel;
using NeuralKernel.Plugins.Core.FileIO;
using NeuralKernel.Plugins.Core.Http;
using NeuralKernel.Plugins.Core.Time;

namespace NeuralKernel.Plugins.Core;

public static class CorePluginExtensions
{
    public static IKernelBuilder AddCorePlugin(this IKernelBuilder builder)
    {
        builder.Plugins.AddFromType<HttpPlugin>("Http");
        builder.Plugins.AddFromType<TimePlugin>("Time");
        builder.Plugins.AddFromType<FileIOPlugin>("FileIO");

        return builder;
    }
}
