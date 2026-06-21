using NeuralKernel.Plugins.Document.Html;
using NeuralKernel.Plugins.Document.Office;
using NeuralKernel.Plugins.Document.Pdf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace NeuralKernel.Plugins.Document;

public static class DocumentPluginExtensions
{
    public static IKernelBuilder AddDocumentPlugin(this IKernelBuilder builder)
    {
        builder.Services.AddSingleton<IDocumentHandler, PdfHandler>();
        builder.Services.AddSingleton<IDocumentHandler, HtmlHandler>();
        builder.Services.AddSingleton<IDocumentHandler, MsWordHandler>();
        builder.Services.AddSingleton<IDocumentHandler, MsExcelHandler>();
        builder.Services.AddSingleton<IDocumentHandler, Text.TextHandler>();
        builder.Services.AddSingleton<IDocumentHandler, Text.JsonHandler>();
        builder.Services.AddSingleton<IDocumentHandler, MsPowerPointHandler>();
        builder.Services.AddSingleton<IDocumentHandler, Text.MarkDownHandler>();
        builder.Services.AddSingleton<ITempFileStorage, MemoryTempFileStorage>();

        builder.Plugins.AddFromType<DocumentPlugin>("Document");

        return builder;
    }
}
