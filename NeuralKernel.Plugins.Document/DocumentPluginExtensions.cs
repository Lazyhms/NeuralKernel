﻿using NeuralKernel.Plugins.Document.Html;
using NeuralKernel.Plugins.Document.Office;
using NeuralKernel.Plugins.Document.Pdf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace NeuralKernel.Plugins.Document;

public static class DocumentPluginExtensions
{
    public static IKernelBuilder AddDocumentPlugin(this IKernelBuilder builder)
    {
        builder.Services.AddSingleton<IFileReader, PdfReader>();
        builder.Services.AddSingleton<IFileReader, HtmlReader>();
        builder.Services.AddSingleton<IFileReader, MsWordReader>();
        builder.Services.AddSingleton<IFileReader, MsExcelReader>();
        builder.Services.AddSingleton<IFileReader, MsPowerPointReader>();
        builder.Services.AddSingleton<IFileReader, Text.TextReader>();
        builder.Services.AddSingleton<IFileReader, Text.MarkDownReader>();

        builder.Services.AddSingleton<IFileWriter, Text.TextWriter>();
        builder.Services.AddSingleton<IFileWriter, Text.MarkDownWriter>();
        builder.Services.AddSingleton<IFileWriter, Text.JsonWriter>();
        builder.Services.AddSingleton<IFileWriter, HtmlWriter>();
        builder.Services.AddSingleton<IFileWriter, MsWordWriter>();
        builder.Services.AddSingleton<IFileWriter, MsExcelWriter>();

        builder.Plugins.AddFromType<DocumentPlugin>();

        return builder;
    }
}
