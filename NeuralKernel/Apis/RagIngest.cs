//using NeuralKernel.StoreDefinition;
//using NeuralKernel.Core.Chunkers;
//using NeuralKernel.Core.DataFormats;
//using NeuralKernel.Core.Pipeline;
//using NeuralKernel.Core.Tiktoken;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.AI;
//using Microsoft.Extensions.Options;
//using Microsoft.SemanticKernel.Connectors.Qdrant;
//using System.Globalization;
//using System.Text;
//using System.Text.Json;

//namespace NeuralKernel.Apis;

//public record RagDocument(string Index, IFormFileCollection Files);

//public static class RagIngest
//{
//    private static readonly PlainTextChunker _plainTextChunker = new(new CL100KTokenizer());
//    private static readonly MarkDownChunker _markDownChunker = new(new CL100KTokenizer());

//    /// <summary>
//    /// 知识库数据摄�?
//    /// </summary>
//    public static RouteGroupBuilder MapRagIngest(this RouteGroupBuilder builder)
//    {
//        builder.MapPost("ingest", async (
//            QdrantVectorStore vectorStore,
//            [FromForm] RagDocument document,
//            IMimeTypeDetection mimeTypeDetection,
//            IEnumerable<IContentDecoder> contentDecoder,
//            IOptionsSnapshot<ModelOptions> optionsSnapshot,
//            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) =>
//        {
//            var stroeCollection = vectorStore.GetCollection<Guid, PolicyDocument>(document.Index);
//            await stroeCollection.EnsureCollectionExistsAsync();

//            foreach (var file in document.Files)
//            {
//                if (!mimeTypeDetection.TryGetFileType(Path.GetFileName(file.FileName), out var mimeType) || string.IsNullOrWhiteSpace(mimeType)) { continue; }

//                var decoder = contentDecoder.LastOrDefault(o => o.SupportsMimeType(mimeType));
//                if (decoder == null) { continue; }

//                using var stream = file.OpenReadStream();
//                var content = await decoder.DecodeAsync(stream);

//                var textBuilder = new StringBuilder();

//                foreach (var section in content.Sections)
//                {
//                    var sectionContent = section.Content.Trim();

//                    if (string.IsNullOrEmpty(sectionContent)) { continue; }

//                    textBuilder.Append(sectionContent);
//                }

//                List<string> chunks;
//                if (content.MimeType == MimeTypes.PlainText)
//                {
//                    chunks = _plainTextChunker.Split(textBuilder.ToString(), new PlainTextChunkerOptions { MaxTokensPerChunk = 512, Overlap = 100 });
//                }
//                else
//                {
//                    chunks = _markDownChunker.Split(textBuilder.ToString(), new MarkDownChunkerOptions { MaxTokensPerChunk = 512, Overlap = 100 });
//                }

//                var fileId = Guid.NewGuid().ToString("N");
//                var pointId = Guid.NewGuid().ToString("N") + DateTimeOffset.Now.ToString("yyyyMMddhhmmssfffffff", CultureInfo.InvariantCulture);
//                foreach (var chunk in chunks)
//                {
//                    var vectors = await embeddingGenerator.GenerateVectorAsync(chunk, new EmbeddingGenerationOptions
//                    {
//                        ModelId = optionsSnapshot.Value.Embedding,
//                    });

//                    await stroeCollection.UpsertAsync(new PolicyDocument
//                    {
//                        VectorId = Guid.NewGuid(),
//                        Id = $"d={pointId}//p={fileId}",
//                        Vectors = vectors,
//                        Tags = [
//                            $"__document_id:{pointId}",
//                            $"__file_type:{mimeType}",
//                            $"__file_id:{fileId}",
//                            $"__file_part:{Guid.NewGuid():N}",
//                            $"__part_n:{chunks.IndexOf(chunk)}",
//                            $"__sect_n:0"
//                        ],
//                        Payload = JsonSerializer.Serialize(new Payload { File = file.FileName, Text = chunk })
//                    });
//                }
//            }
//        }).WithSummary("知识库数据摄�?).DisableAntiforgery();

//        return builder;
//    }
//}
