using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System.Text;

namespace NeuralKernel.Plugins.Document.Office;

public sealed class MsPowerPointReader : IFileReader
{
    public IReadOnlyList<string> MimeType { get; } =
        [
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation"
        ];

    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        var readerContent = new StringBuilder();
        using var presentationDocument = PresentationDocument.Open(data, false);

        if (presentationDocument.PresentationPart is PresentationPart presentationPart
            && presentationPart.Presentation is Presentation presentation
            && presentation.SlideIdList is SlideIdList slideIdList
            && slideIdList.Elements<SlideId>().ToList() is List<SlideId> slideIds and { Count: > 0 })
        {
            foreach (var slideId in slideIds)
            {
                if ((string?)slideId.RelationshipId is string relationshipId
                    && presentationPart.GetPartById(relationshipId) is SlidePart slidePart
                    && slidePart != null
                    && slidePart.Slide?.Descendants<DocumentFormat.OpenXml.Drawing.Text>().ToList() is List<DocumentFormat.OpenXml.Drawing.Text> texts and { Count: > 0 })
                {
                    bool isVisible = slidePart.Slide.Show ?? true;
                    if (!isVisible) { continue; }

                    var currentSlideContent = new StringBuilder();
                    for (var i = 0; i < texts.Count; i++)
                    {
                        var text = texts[i];
                        currentSlideContent.Append(text.Text);
                        if (i < texts.Count - 1)
                        {
                            currentSlideContent.Append(' ');
                        }
                    }

                    if (currentSlideContent.Length < 1) { continue; }

                    readerContent.Append(currentSlideContent);
                    readerContent.AppendLine(string.Empty);
                }
            }
        }

        return await Task.FromResult(readerContent.ToString());
    }
}
