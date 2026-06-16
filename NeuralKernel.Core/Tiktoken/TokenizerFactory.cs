namespace NeuralKernel.Core.Tiktoken;

public static class TokenizerFactory
{
    public static ITextTokenizer? GetTokenizerForEncoding(string encodingId)
    {
        encodingId = encodingId.ToLowerInvariant();

        return encodingId.ToLowerInvariant() switch
        {
            "p50k" => new P50KTokenizer(),
            "cl100k" => new CL100KTokenizer(),
            "o200k" => new O200KTokenizer(),
            _ => null,
        };
    }

    public static ITextTokenizer? GetTokenizerForModel(string modelId)
    {
        try
        {
            return new TiktokenTokenizer(modelId);
        }
        catch (Exception)
        {
            // ignore
        }

        modelId = modelId.ToLowerInvariant();

        if (modelId.StartsWith("text-embedding-", StringComparison.Ordinal)
            || modelId.StartsWith("gpt-3.5-", StringComparison.Ordinal)
            || modelId.StartsWith("gpt-4-", StringComparison.Ordinal))
        {
            return new CL100KTokenizer();
        }

        if (modelId.StartsWith("gpt-4o-", StringComparison.Ordinal))
        {
            return new O200KTokenizer();
        }

        return modelId.ToLowerInvariant() switch
        {
            "code-davinci-001" or "code-davinci-002" or "text-davinci-002" or "text-davinci-003" => new P50KTokenizer(),
            "gpt-3.5-turbo" or "gpt-4" => new CL100KTokenizer(),
            "gpt-4o" => new O200KTokenizer(),
            _ => null,
        };
    }
}
