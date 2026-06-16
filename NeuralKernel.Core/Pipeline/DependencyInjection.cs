using Microsoft.Extensions.DependencyInjection;

namespace NeuralKernel.Core.Pipeline;

public static partial class DependencyInjection
{
    public static IServiceCollection AddDefaultMimeTypeDetection(
        this IServiceCollection services)
    {
        return services.AddSingleton<IMimeTypeDetection, MimeTypesDetection>();
    }
}
