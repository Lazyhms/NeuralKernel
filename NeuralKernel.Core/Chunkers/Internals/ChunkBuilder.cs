using System.Text;

namespace NeuralKernel.Core.Chunkers.Internals;

internal class ChunkBuilder
{
    public readonly StringBuilder FullContent = new();
    public readonly StringBuilder NextSentence = new();
}
