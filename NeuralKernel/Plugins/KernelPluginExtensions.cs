using Microsoft.SemanticKernel;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeuralKernel.Plugins;

public static class KernelPluginExtensions
{
    internal static readonly MethodInfo AddFromTypeMethodInfo
        = typeof(Microsoft.SemanticKernel.KernelExtensions).GetTypeInfo().GetDeclaredMethods(nameof(KernelExtensions.AddFromType))
            .Single(mi =>
            {
                var p = mi.GetParameters();
                return mi.ReturnType == typeof(IKernelBuilderPlugins) && p.Length == 3
                    && p.Any(pi => pi.Name == "jsonSerializerOptions" && pi.ParameterType == typeof(JsonSerializerOptions))
                    && p.Any(pi => pi.Name == "pluginName" && pi.ParameterType == typeof(string));
            });

    internal static readonly MethodInfo ImportPluginFromTypeMethodInfo
        = typeof(Microsoft.SemanticKernel.KernelExtensions).GetTypeInfo().GetDeclaredMethods(nameof(KernelExtensions.ImportPluginFromType))
            .Single(mi =>
            {
                var p = mi.GetParameters();
                return mi.ReturnType == typeof(KernelPlugin) && p.Length == 3
                    && p.Any(pi => pi.Name == "jsonSerializerOptions" && pi.ParameterType == typeof(JsonSerializerOptions))
                    && p.Any(pi => pi.Name == "pluginName" && pi.ParameterType == typeof(string));
            });

    private static readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static IEnumerable<FileInfo> GetPluginFileInfo(string? pluginDirectory)
    {
        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            pluginDirectory = "Plugins";
        }

        if (!Path.IsPathFullyQualified(pluginDirectory))
        {
            pluginDirectory = Path.Combine(AppContext.BaseDirectory, pluginDirectory);
        }

        return Directory.CreateDirectory(pluginDirectory).EnumerateFiles("*.dll", new EnumerationOptions
        {
            MaxRecursionDepth = 10,
            IgnoreInaccessible = true,
            MatchType = MatchType.Win32,
            RecurseSubdirectories = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
        });
    }

    public static void ImportPluginFromDirectory(this Kernel kernel, string? pluginDirectory = null)
    {
        foreach (var item in GetPluginFileInfo(pluginDirectory))
        {
            var loadContext = new KernelPluginLoadContext(item.FullName);
            var assembly = loadContext.LoadFromAssemblyPath(item.FullName);
            foreach (var pluginType in assembly.GetTypes().Where(t => t.IsPublic && t.IsClass && !t.IsAbstract))
            {
                var kernelPluginAttribute = pluginType.GetCustomAttribute<KernelPluginAttribute>();
                if (kernelPluginAttribute != null && kernelPluginAttribute.Enable)
                {
                    var genericMethod = ImportPluginFromTypeMethodInfo.MakeGenericMethod(pluginType);
                    var plugin = (KernelPlugin)genericMethod.Invoke(null, [kernel, jsonSerializerOptions, kernelPluginAttribute.Name])!;
                }
            }
        }
    }

    public static IKernelBuilderPlugins AddFromPluginDirectory(this IKernelBuilderPlugins plugins, string? pluginDirectory = null)
    {
        foreach (var item in GetPluginFileInfo(pluginDirectory))
        {
            var loadContext = new KernelPluginLoadContext(item.FullName);
            var assembly = loadContext.LoadFromAssemblyPath(item.FullName);
            foreach (var pluginType in assembly.GetTypes().Where(t => t.IsPublic && t.IsClass && !t.IsAbstract))
            {
                var kernelPluginAttribute = pluginType.GetCustomAttribute<KernelPluginAttribute>();
                if (kernelPluginAttribute != null && kernelPluginAttribute.Enable)
                {
                    var genericMethod = AddFromTypeMethodInfo.MakeGenericMethod(pluginType);
                    genericMethod.Invoke(null, [plugins, jsonSerializerOptions, kernelPluginAttribute.Name]);
                }
            }
        }

        return plugins;
    }
}