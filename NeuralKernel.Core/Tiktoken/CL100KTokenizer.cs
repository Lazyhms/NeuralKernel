using Microsoft.ML.Tokenizers;

namespace NeuralKernel.Core.Tiktoken;

public class CL100KTokenizer : ITextTokenizer
{
    private static readonly Tokenizer s_tokenizer = Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForEncoding("cl100k_base",
        new Dictionary<string, int> { { "<|im_start|>", 100264 }, { "<|im_end|>", 100265 } });

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
