using Microsoft.ML.Tokenizers;

namespace NeuralKernel.Core.Tiktoken;

public class TiktokenTokenizer : ITextTokenizer
{
    private readonly Tokenizer _tokenizer;

    public TiktokenTokenizer(string modelId)
    {
        try
        {
            _tokenizer = Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForModel(modelId);
        }
        catch (NotSupportedException)
        {
            throw new Exception("Autodetect failed");
        }
        catch (ArgumentNullException)
        {
            throw new Exception("Autodetect failed");
        }
    }

    public int CountTokens(string text)
    {
        return _tokenizer.CountTokens(text);
    }

    public IReadOnlyList<string> GetTokens(string text) => [.. _tokenizer.EncodeToTokens(text, out string? _).Select(t => t.Value)];
}
