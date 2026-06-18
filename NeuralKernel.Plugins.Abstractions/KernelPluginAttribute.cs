#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.SemanticKernel;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class KernelPluginAttribute(string? name) : Attribute
{
    public KernelPluginAttribute() : this(null)
    {
    }

    public string? Name { get; } = name;

    public bool Enable { get; set; } = true;
}
