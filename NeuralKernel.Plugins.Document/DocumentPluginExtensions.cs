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
        builder.Services.AddSingleton<IFileHandler, PdfHandler>();
        builder.Services.AddSingleton<IFileHandler, HtmlHandler>();
        builder.Services.AddSingleton<IFileHandler, MsWordHandler>();
        builder.Services.AddSingleton<IFileHandler, MsExcelHandler>();
        builder.Services.AddSingleton<IFileHandler, Text.TextHandler>();
        builder.Services.AddSingleton<IFileHandler, Text.JsonHandler>();
        builder.Services.AddSingleton<IFileHandler, MsPowerPointHandler>();
        builder.Services.AddSingleton<IFileHandler, Text.MarkDownHandler>();

        builder.Plugins.AddFromType<DocumentPlugin>("Document");

        return builder;
    }
}
