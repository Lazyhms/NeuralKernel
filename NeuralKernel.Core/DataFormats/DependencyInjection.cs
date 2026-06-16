using NeuralKernel.Core.DataFormats.Office;
using NeuralKernel.Core.DataFormats.Pdf;
using NeuralKernel.Core.DataFormats.Text;
using NeuralKernel.Core.DataFormats.WebPages;
using Microsoft.Extensions.DependencyInjection;

namespace NeuralKernel.Core.DataFormats;

public static partial class DependencyInjection
{
    public static IServiceCollection AddContentDecoder<T>(
        this IServiceCollection services) where T : class, IContentDecoder
    {
        services.AddSingleton<IContentDecoder, T>();
        return services;
    }

    public static IServiceCollection AddContentDecoder(
        this IServiceCollection services, IContentDecoder decoder)
    {
        services.AddSingleton(decoder);
        return services;
    }

    public static IServiceCollection AddDefaultContentDecoders(
        this IServiceCollection services)
    {
        services.AddSingleton<IContentDecoder, TextDecoder>();
        services.AddSingleton<IContentDecoder, MarkDownDecoder>();
        services.AddSingleton<IContentDecoder, HtmlDecoder>();
        services.AddSingleton<IContentDecoder, PdfDecoder>();
        services.AddSingleton<IContentDecoder, MsExcelDecoder>();
        services.AddSingleton<IContentDecoder, MsPowerPointDecoder>();
        services.AddSingleton<IContentDecoder, MsWordDecoder>();

        return services;
    }

    public static IServiceCollection AddDefaultWebScraper(
        this IServiceCollection services)
    {
        return services.AddSingleton<IWebScraper, WebScraper>();
    }

    public static IServiceCollection AddCustomWebScraper(
        this IServiceCollection services, IWebScraper webScraper)
    {
        return services.AddSingleton(webScraper);
    }

    public static IServiceCollection AddCustomWebScraper<T>(
        this IServiceCollection services) where T : class, IWebScraper
    {
        return services.AddSingleton<IWebScraper, T>();
    }
}
