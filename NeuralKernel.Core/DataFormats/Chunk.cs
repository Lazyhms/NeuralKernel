using System.Text;
using System.Text.Json.Serialization;

namespace NeuralKernel.Core.DataFormats;

public class Chunk(string text, int number)
{
    [JsonPropertyOrder(0)]
    [JsonPropertyName("number")]
    public int Number { get; } = number;

    /// <summary>
    /// Page text content
    /// </summary>
    [JsonPropertyOrder(1)]
    [JsonPropertyName("content")]
    public string Content { get; set; } = text ?? string.Empty;

    [JsonIgnore]
    public bool IsSeparator { get; set; }

    public Chunk(char text, int number) : this(text.ToString(), number)
    {
    }

    public Chunk(StringBuilder text, int number) : this(text.ToString(), number)
    {
    }
}
