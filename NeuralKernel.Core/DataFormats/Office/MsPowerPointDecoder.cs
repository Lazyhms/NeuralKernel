using NeuralKernel.Core.Pipeline;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Logging;
using System.Text;

namespace NeuralKernel.Core.DataFormats.Office;

public sealed class MsPowerPointDecoder(ILoggerFactory loggerFactory, MsPowerPointDecoderConfig? config = null) : IContentDecoder
{
    private readonly MsPowerPointDecoderConfig _config = config ?? new MsPowerPointDecoderConfig();
    private readonly ILogger<MsPowerPointDecoder> _log = loggerFactory.CreateLogger<MsPowerPointDecoder>();

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
        => mimeType != null && mimeType.StartsWith(MimeTypes.MsPowerPointX, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(string filename, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filename);
        return DecodeAsync(stream, cancellationToken);
    }

    public Task<FileContent> DecodeAsync(BinaryData data, CancellationToken cancellationToken = default)
    {
        using var stream = data.ToStream();
        return DecodeAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(Stream data, CancellationToken cancellationToken = default)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Extracting text from MS PowerPoint file");
        }

        var result = new FileContent(MimeTypes.PlainText);
        using PresentationDocument presentationDocument = PresentationDocument.Open(data, false);
        var sb = new StringBuilder();

        if (presentationDocument.PresentationPart is PresentationPart presentationPart
            && presentationPart.Presentation is Presentation presentation
            && presentation.SlideIdList is SlideIdList slideIdList
            && slideIdList.Elements<SlideId>().ToList() is List<SlideId> slideIds and { Count: > 0 })
        {
            var slideNumber = 0;
            foreach (SlideId slideId in slideIds)
            {
                slideNumber++;
                if ((string?)slideId.RelationshipId is string relationshipId
                    && presentationPart.GetPartById(relationshipId) is SlidePart slidePart
                    && slidePart != null
                    && slidePart.Slide?.Descendants<DocumentFormat.OpenXml.Drawing.Text>().ToList() is List<DocumentFormat.OpenXml.Drawing.Text> texts and { Count: > 0 })
                {
                    // Check if the slide is hidden and whether to skip it
                    // PowerPoint does not set the value of this property, in general, unless the slide is to be hidden
                    // The only way the Show property would exist and have a value of true would be if the slide had been hidden and then unhidden
                    // - Show is null: default, slide is visible
                    // - Show is false: the slide is hidden
                    // - Show is true: the slide is visible
                    bool isVisible = slidePart.Slide.Show ?? true;
                    if (_config.SkipHiddenSlides && !isVisible) { continue; }

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

                    // Skip the slide if there is no text
                    if (currentSlideContent.Length < 1) { continue; }

                    // Prepend slide number before the slide text
                    if (_config.WithSlideNumber)
                    {
                        sb.AppendLineNix(_config.SlideNumberTemplate.Replace("{number}", $"{slideNumber}", StringComparison.OrdinalIgnoreCase));
                    }

                    sb.Append(currentSlideContent);
                    sb.AppendLineNix();

                    // Append the end of slide marker
                    if (_config.WithEndOfSlideMarker)
                    {
                        sb.AppendLineNix(_config.EndOfSlideMarkerTemplate.Replace("{number}", $"{slideNumber}", StringComparison.OrdinalIgnoreCase));
                    }
                }

                string slideContent = sb.ToString().NormalizeNewlines(true);
                sb.Clear();
                result.Sections.Add(new Chunk(slideContent, slideNumber));
            }
        }

        return Task.FromResult(result);
    }
}
