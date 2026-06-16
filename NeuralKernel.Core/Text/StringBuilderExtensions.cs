

using System.Text;

namespace NeuralKernel.Core.Text;

public static class StringBuilderExtensions
{
    /// <summary>
    /// Append line using Unix line ending "\n"
    /// </summary>
    public static void AppendLineNix(this StringBuilder sb)
    {
        sb.Append('\n');
    }

    /// <summary>
    /// Append line using Unix line ending "\n"
    /// </summary>
    public static void AppendLineNix(this StringBuilder sb, string value)
    {
        sb.Append(value);
        sb.Append('\n');
    }

    /// <summary>
    /// Append line using Unix line ending "\n"
    /// </summary>
    public static void AppendLineNix(this StringBuilder sb, char value)
    {
        sb.Append(value);
        sb.Append('\n');
    }

    /// <summary>
    /// Append line using Unix line ending "\n"
    /// </summary>
    public static void AppendLineNix(this StringBuilder sb, StringBuilder value)
    {
        sb.Append(value);
        sb.Append('\n');
    }
}
