using NeuralKernel.Core.Chunkers.Internals;
using NeuralKernel.Core.DataFormats;
using NeuralKernel.Core.Tiktoken;
using System.Text;

namespace NeuralKernel.Core.Chunkers;

/// <summary>
/// Plain text chunker for splitting text into blocks of a maximum number of tokens.
/// Designed for Plain Text and RAG scenarios, where some special chars are irrelevant
/// and can be removed, ie. the split can be lossy.
/// This chunker should not be used for MarkDown, where symbols have a special meaning,
/// or different priorities for splitting.
/// Although not designed to chunk source code or math formulas, it tries to do its best.
/// Acronyms with dots (e.g. N.A.S.A.) are not considered and are potentially split like sentences.
/// Anomalous-long sentences are split during the chunking loop, potentially introducing noise at the start of following chunks.
/// When using overlapping, the resulting chunk size might be larger than the specified max size, due to how LLM tokenizers work.
/// </summary>
public class PlainTextChunker(ITextTokenizer? tokenizer = null)
{
    internal enum SeparatorTypes
    {
        ExplicitSeparator,
        PotentialSeparator,
        WeakSeparator1,
        WeakSeparator2,
        WeakSeparator3,
        NotASeparator,
    }

    private const int MinChunkSize = 5;

    private readonly ITextTokenizer _tokenizer = tokenizer ?? new CL100KTokenizer();

    private static readonly SeparatorTrie s_explicitSeparators = new([
        ". ", ".\t", ".\n", "\n\n",
        "? ", "?\t", "?\n",
        "! ", "!\t", "!\n",
        "？ ", "？\t", "？\n",
        "！ ", "！\t", "！\n",
        "。 ", "。\t", "。\n",
        "!!!!", "????", "!!!", "???", "?!?", "!?!", "!?", "?!", "!!", "??", "....", "...", "..",
        ".", "?", "!", "？", "！", "。",
        "！", "？", "。", "；", "，"
    ]);

    private static readonly SeparatorTrie s_potentialSeparators = new([
        "; ", ";\t", ";\n", ";",
        "} ", "}\t", "}\n", "}",
        ") ", ")\t", ")\n",
        "] ", "]\t", "]\n",
        ")", "]",
        "；", "】", "）", "］", "”", "’", "》", "〉", "】", "）"
    ]);

    private static readonly SeparatorTrie s_weakSeparators1 = new([
        ": ", ":",
        ", ", ",",
        "：", "，"
    ]);

    private static readonly SeparatorTrie s_weakSeparators2 = new([
        "\n",
        "\t",
        "' ", "'",
        "\" ", "\"",
        " ",
        "、", "·", "·", "·"
    ]);

    private static readonly SeparatorTrie s_weakSeparators3 = new([
        "_",
        "-",
        "|",
        "@",
        "=",
        "—", "…", "·"
    ]);

    public List<string> Split(string text, int maxTokensPerChunk)
    {
        return Split(text, new PlainTextChunkerOptions { MaxTokensPerChunk = maxTokensPerChunk });
    }

    public List<string> Split(string text, PlainTextChunkerOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        text = text.NormalizeNewlines(true);

        int maxChunk1Size = options.MaxTokensPerChunk - TokenCount(options.ChunkHeader);
        int maxChunkNSize = options.MaxTokensPerChunk - TokenCount(options.ChunkHeader) - options.Overlap;
        maxChunk1Size = Math.Max(MinChunkSize, maxChunk1Size);
        maxChunkNSize = Math.Max(MinChunkSize, maxChunkNSize);

        bool firstChunkDone = false;
        var chunks = RecursiveSplit(text, maxChunk1Size, maxChunkNSize, SeparatorTypes.ExplicitSeparator, ref firstChunkDone);

        if (options.Overlap > 0 && chunks.Count > 1)
        {
            var newChunks = new List<string> { chunks[0] };

            for (int index = 1; index < chunks.Count; index++)
            {
                IReadOnlyList<string> previousChunkTokens = _tokenizer.GetTokens(chunks[index - 1]);
                IEnumerable<string> overlapTokens = previousChunkTokens.Skip(previousChunkTokens.Count - options.Overlap);
                newChunks.Add($"{string.Join("", overlapTokens)}{chunks[index]}");
            }

            chunks = newChunks;
        }

        if (!string.IsNullOrEmpty(options.ChunkHeader))
        {
            chunks = [.. chunks.Select(x => $"{options.ChunkHeader}{x}")];
        }

#if DEBUGCHUNKS
        this.DebugChunks(chunks);
#endif

        return chunks;
    }

    internal static SeparatorTypes NextSeparatorType(SeparatorTypes separatorType) => separatorType switch
    {
        SeparatorTypes.ExplicitSeparator => SeparatorTypes.PotentialSeparator,
        SeparatorTypes.PotentialSeparator => SeparatorTypes.WeakSeparator1,
        SeparatorTypes.WeakSeparator1 => SeparatorTypes.WeakSeparator2,
        SeparatorTypes.WeakSeparator2 => SeparatorTypes.WeakSeparator3,
        SeparatorTypes.WeakSeparator3 => SeparatorTypes.NotASeparator,
        SeparatorTypes.NotASeparator or
        _ => throw new ArgumentOutOfRangeException($"{nameof(SeparatorTypes.NotASeparator).ToLower()} doesn't have a next separator type."),
    };

    internal List<string> RecursiveSplit(
        string text,
        int maxChunk1Size,
        int maxChunkNSize,
        SeparatorTypes separatorType,
        ref bool firstChunkDone)
    {
#if DEBUGRECURSION
        Console.WriteLine($"RecursiveSplit: {text.Length} chars; maxChunk1Size: {maxChunk1Size}; maxChunkNSize: {maxChunkNSize}; separatorType: {separatorType:G}");
#endif
        if (string.IsNullOrEmpty(text)) { return []; }

        var maxChunkSize = firstChunkDone ? maxChunkNSize : maxChunk1Size;
        if (TokenCount(text) <= maxChunkSize) { return [text]; }

        List<Chunk> fragments = separatorType switch
        {
            SeparatorTypes.ExplicitSeparator => SplitToFragments(text, s_explicitSeparators),
            SeparatorTypes.PotentialSeparator => SplitToFragments(text, s_potentialSeparators),
            SeparatorTypes.WeakSeparator1 => SplitToFragments(text, s_weakSeparators1),
            SeparatorTypes.WeakSeparator2 => SplitToFragments(text, s_weakSeparators2),
            SeparatorTypes.WeakSeparator3 => SplitToFragments(text, s_weakSeparators3),
            SeparatorTypes.NotASeparator => SplitToFragments(text, null),
            _ => throw new ArgumentOutOfRangeException(nameof(separatorType), separatorType, null)
        };

        return GenerateChunks(fragments, maxChunk1Size, maxChunkNSize, separatorType, ref firstChunkDone);
    }

    internal List<string> GenerateChunks(
        List<Chunk> fragments,
        int maxChunk1Size,
        int maxChunkNSize,
        SeparatorTypes separatorType,
        ref bool firstChunkDone)
    {
        if (fragments.Count == 0) { return []; }

        var chunks = new List<string>();
        var chunk = new ChunkBuilder();
        int maxChunkSize;

        foreach (var fragment in fragments)
        {
            chunk.NextSentence.Append(fragment.Content);

            if (!fragment.IsSeparator) { continue; }

            string nextSentence = chunk.NextSentence.ToString();
            int nextSentenceSize = TokenCount(nextSentence);
            maxChunkSize = firstChunkDone ? maxChunkNSize : maxChunk1Size;

            int state;
            if (chunk.FullContent.Length == 0)
            {
                state = (nextSentenceSize <= maxChunkSize) ? 1 : 2;
            }
            else
            {
                state = (nextSentenceSize <= maxChunkSize) ? 3 : 4;
            }

            switch (state)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(state).ToLower());

                case 1:
                    chunk.FullContent.Append(nextSentence);
                    chunk.NextSentence.Clear();
                    continue;

                case 2:
                    {
                        var moreChunks = RecursiveSplit(nextSentence, maxChunk1Size, maxChunkNSize, NextSeparatorType(separatorType), ref firstChunkDone);
                        chunks.AddRange(moreChunks.Take(moreChunks.Count - 1));
                        chunk.NextSentence.Clear().Append(moreChunks.Last());
                        continue;
                    }

                case 3:
                    {
                        var chunkPlusSentence = $"{chunk.FullContent}{chunk.NextSentence}";
                        if (TokenCount(chunkPlusSentence) <= maxChunkSize)
                        {
                            chunk.FullContent.Append(chunk.NextSentence);
                        }
                        else
                        {
                            AddChunk(chunks, chunk.FullContent.ToString(), ref firstChunkDone);
                            chunk.FullContent.Clear().Append(chunk.NextSentence);
                        }

                        chunk.NextSentence.Clear();
                        continue;
                    }

                case 4:
                    {
                        AddChunk(chunks, chunk.FullContent, ref firstChunkDone);

                        var moreChunks = RecursiveSplit(nextSentence, maxChunk1Size, maxChunkNSize, NextSeparatorType(separatorType), ref firstChunkDone);
                        chunks.AddRange(moreChunks.Take(moreChunks.Count - 1));
                        chunk.NextSentence.Clear().Append(moreChunks.Last());
                        continue;
                    }
            }
        }

        string fullSentenceLeft = chunk.FullContent.ToString();
        string nextSentenceLeft = chunk.NextSentence.ToString();
        maxChunkSize = firstChunkDone ? maxChunkNSize : maxChunk1Size;

        if (fullSentenceLeft.Length > 0 || nextSentenceLeft.Length > 0)
        {
            if (TokenCount($"{fullSentenceLeft}{nextSentenceLeft}") <= maxChunkSize)
            {
                AddChunk(chunks, $"{fullSentenceLeft}{nextSentenceLeft}", ref firstChunkDone);
            }
            else
            {
                if (fullSentenceLeft.Length > 0)
                {
                    AddChunk(chunks, fullSentenceLeft, ref firstChunkDone);
                }

                if (nextSentenceLeft.Length > 0)
                {
                    if (TokenCount(nextSentenceLeft) < maxChunkSize)
                    {
                        AddChunk(chunks, nextSentenceLeft, ref firstChunkDone);
                    }
                    else
                    {
                        var moreChunks = RecursiveSplit(nextSentenceLeft, maxChunk1Size, maxChunkNSize, NextSeparatorType(separatorType), ref firstChunkDone);
                        chunks.AddRange(moreChunks);
                    }
                }
            }
        }

        return chunks;
    }

    internal static List<Chunk> SplitToFragments(string text, SeparatorTrie? separators)
    {
        if (separators == null)
        {
            return [.. text.Select(x => new Chunk(x, -1) { IsSeparator = true })];
        }

        if (string.IsNullOrEmpty(text) || separators.Length == 0) { return []; }

        var fragments = new List<Chunk>();
        var fragmentBuilder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            string? foundSeparator = separators.MatchLongest(text, index);

            if (foundSeparator != null)
            {
                if (fragmentBuilder.Length > 0)
                {
                    fragments.Add(new Chunk(fragmentBuilder, -1) { IsSeparator = false });
                    fragmentBuilder.Clear();
                }

                fragments.Add(new Chunk(foundSeparator, -1) { IsSeparator = true });
                index += foundSeparator.Length;
            }
            else
            {
                fragmentBuilder.Append(text[index]);
                index++;
            }
        }

        if (fragmentBuilder.Length > 0)
        {
            fragments.Add(new Chunk(fragmentBuilder, -1) { IsSeparator = false });
        }

#if DEBUGFRAGMENTS
        this.DebugFragments(fragments);
#endif

        return fragments;
    }

    private int TokenCount(string? input)
    {
        if (input == null) { return 0; }

        return _tokenizer.CountTokens(input);
    }

    private static void AddChunk(List<string> chunks, StringBuilder chunk, ref bool firstChunkDone)
    {
        chunks.Add(chunk.ToString());
        chunk.Clear();
        firstChunkDone = true;
    }

    private static void AddChunk(List<string> chunks, string chunk, ref bool firstChunkDone)
    {
        chunks.Add(chunk);
        firstChunkDone = true;
    }

    #region internals

#if DEBUGCHUNKS
    private void DebugChunks(List<string> chunks)
    {
        Console.WriteLine("-CHUNKS---------------------------");
        if (chunks.Count == 0)
        {
            Console.WriteLine("No chunks in the list");
        }

        for (int index = 0; index < chunks.Count; index++)
        {
            Console.WriteLine($"- {index}: \"{chunks[index]}\" [{this.TokenCount(chunks[index])} tokens]");
        }

        Console.WriteLine("----------------------------------");
    }
#endif

#if DEBUGFRAGMENTS
    private void DebugFragments(List<Fragment> fragments)
    {
        Console.WriteLine("-FRAGMENTS-----------------------------");
        if (fragments.Count == 0)
        {
            Console.WriteLine("No fragments in the list");
        }

        for (int index = 0; index < fragments.Count; index++)
        {
            Fragment fragment = fragments[index];
            Console.WriteLine($"- {index}: \"{fragment.Content}\"");
        }

        Console.WriteLine("---------------------------------------");
    }
#endif

    #endregion
}
