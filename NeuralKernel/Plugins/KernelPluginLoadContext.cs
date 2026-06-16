using Microsoft.SemanticKernel;
using System.Reflection;
using System.Runtime.Loader;

namespace NeuralKernel.Plugins;

public sealed class KernelPluginLoadContext(string pluginDllPath) : AssemblyLoadContext(true)
{
    private readonly AssemblyDependencyResolver _resolver = new(pluginDllPath);

    private static readonly HashSet<string> _sharedAssemblyNames = [typeof(KernelPluginAttribute).Assembly.GetName().Name!];

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (_sharedAssemblyNames.Contains(assemblyName.Name!))
        {
            return null;
        }

        if (Default.Assemblies.Any(a => string.Equals(a.GetName().Name, assemblyName.Name)))
        {
            return null;
        }

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}