using Microsoft.ML.Tokenizers;

namespace NeuralKernel.Core.Tiktoken;

public class P50KTokenizer : ITextTokenizer
{
    private static readonly Tokenizer s_tokenizer = Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForEncoding("p50k_base");

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        return s_tokenizer.CountTokens(text);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetTokens(string text)
    {
        return [.. s_tokenizer.EncodeToTokens(text, out string? _).Select(t => t.Value)];
    }
}
