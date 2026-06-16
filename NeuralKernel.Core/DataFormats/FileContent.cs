using System.Text.Json.Serialization;

namespace NeuralKernel.Core.DataFormats;

public class FileContent(string mimeType)
{
    [JsonPropertyOrder(0)]
    [JsonPropertyName("sections")]
    public List<Chunk> Sections { get; set; } = [];

    [JsonPropertyOrder(1)]
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = mimeType;
}
