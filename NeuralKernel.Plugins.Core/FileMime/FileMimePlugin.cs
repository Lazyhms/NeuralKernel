using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace NeuralKernel.Plugins.Core.FileMime;

/// <summary>
/// 提供文件扩展名与 MIME 类型之间的映射功能。
/// </summary>
public sealed class FileMimePlugin
{
    private static readonly Dictionary<string, string> s_extensionToMimeType =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { FileExtensions.PlainText, MimeTypes.PlainText },

            { FileExtensions.MarkDown, MimeTypes.MarkDown },

            { FileExtensions.Htm, MimeTypes.Html },
            { FileExtensions.Html, MimeTypes.Html },
            { FileExtensions.XHTML, MimeTypes.XHTML },
            { FileExtensions.XML, MimeTypes.XML },
            { FileExtensions.JSONLD, MimeTypes.JSONLD },
            { FileExtensions.CascadingStyleSheet, MimeTypes.CascadingStyleSheet },
            { FileExtensions.JavaScript, MimeTypes.JavaScript },
            { FileExtensions.BourneShellScript, MimeTypes.BourneShellScript },

            { FileExtensions.ImageBmp, MimeTypes.ImageBmp },
            { FileExtensions.ImageGif, MimeTypes.ImageGif },
            { FileExtensions.ImageJpeg, MimeTypes.ImageJpeg },
            { FileExtensions.ImageJpg, MimeTypes.ImageJpeg },
            { FileExtensions.ImagePng, MimeTypes.ImagePng },
            { FileExtensions.ImageTiff, MimeTypes.ImageTiff },
            { FileExtensions.ImageTiff2, MimeTypes.ImageTiff },
            { FileExtensions.ImageWebP, MimeTypes.ImageWebP },
            { FileExtensions.ImageSVG, MimeTypes.ImageSVG },

            { FileExtensions.WebPageUrl, MimeTypes.WebPageUrl },
            { FileExtensions.TextEmbeddingVector, MimeTypes.TextEmbeddingVector },
            { FileExtensions.Json, MimeTypes.Json },
            { FileExtensions.CSVData, MimeTypes.CSVData },

            { FileExtensions.Pdf, MimeTypes.Pdf },
            { FileExtensions.RTFDocument, MimeTypes.RTFDocument },

            { FileExtensions.MsWord, MimeTypes.MsWord },
            { FileExtensions.MsWordX, MimeTypes.MsWordX },
            { FileExtensions.MsPowerPoint, MimeTypes.MsPowerPoint },
            { FileExtensions.MsPowerPointX, MimeTypes.MsPowerPointX },
            { FileExtensions.MsExcel, MimeTypes.MsExcel },
            { FileExtensions.MsExcelX, MimeTypes.MsExcelX },

            { FileExtensions.OpenDocumentText, MimeTypes.OpenDocumentText },
            { FileExtensions.OpenDocumentSpreadsheet, MimeTypes.OpenDocumentSpreadsheet },
            { FileExtensions.OpenDocumentPresentation, MimeTypes.OpenDocumentPresentation },
            { FileExtensions.ElectronicPublicationZip, MimeTypes.ElectronicPublicationZip },

            { FileExtensions.AudioAAC, MimeTypes.AudioAAC },
            { FileExtensions.AudioMP3, MimeTypes.AudioMP3 },
            { FileExtensions.AudioWaveform, MimeTypes.AudioWaveform },
            { FileExtensions.AudioOGG, MimeTypes.AudioOGG },
            { FileExtensions.AudioOpus, MimeTypes.AudioOpus },
            { FileExtensions.AudioWEBM, MimeTypes.AudioWEBM },

            { FileExtensions.VideoMP4, MimeTypes.VideoMP4 },
            { FileExtensions.VideoMPEG, MimeTypes.VideoMPEG },
            { FileExtensions.VideoOGG, MimeTypes.VideoOGG },
            { FileExtensions.VideoOGGGeneric, MimeTypes.VideoOGGGeneric },
            { FileExtensions.VideoWEBM, MimeTypes.VideoWEBM },

            { FileExtensions.ArchiveTar, MimeTypes.ArchiveTar },
            { FileExtensions.ArchiveGzip, MimeTypes.ArchiveGzip },
            { FileExtensions.ArchiveZip, MimeTypes.ArchiveZip },
            { FileExtensions.ArchiveRar, MimeTypes.ArchiveRar },
            { FileExtensions.Archive7Zip, MimeTypes.Archive7Zip },
        };

    private static readonly Dictionary<string, string> s_mimeTypeToExtension =
        new(s_extensionToMimeType.Count, StringComparer.OrdinalIgnoreCase)
        {
            { MimeTypes.PlainText, FileExtensions.PlainText },
            { MimeTypes.MarkDown, FileExtensions.MarkDown },
            { MimeTypes.Html, FileExtensions.Html },
            { MimeTypes.XHTML, FileExtensions.XHTML },
            { MimeTypes.XML, FileExtensions.XML },
            { MimeTypes.JSONLD, FileExtensions.JSONLD },
            { MimeTypes.CascadingStyleSheet, FileExtensions.CascadingStyleSheet },
            { MimeTypes.JavaScript, FileExtensions.JavaScript },
            { MimeTypes.BourneShellScript, FileExtensions.BourneShellScript },
            { MimeTypes.ImageBmp, FileExtensions.ImageBmp },
            { MimeTypes.ImageGif, FileExtensions.ImageGif },
            { MimeTypes.ImageJpeg, FileExtensions.ImageJpeg },
            { MimeTypes.ImagePng, FileExtensions.ImagePng },
            { MimeTypes.ImageTiff, FileExtensions.ImageTiff },
            { MimeTypes.ImageWebP, FileExtensions.ImageWebP },
            { MimeTypes.ImageSVG, FileExtensions.ImageSVG },
            { MimeTypes.WebPageUrl, FileExtensions.WebPageUrl },
            { MimeTypes.TextEmbeddingVector, FileExtensions.TextEmbeddingVector },
            { MimeTypes.Json, FileExtensions.Json },
            { MimeTypes.CSVData, FileExtensions.CSVData },
            { MimeTypes.Pdf, FileExtensions.Pdf },
            { MimeTypes.RTFDocument, FileExtensions.RTFDocument },
            { MimeTypes.MsWord, FileExtensions.MsWord },
            { MimeTypes.MsWordX, FileExtensions.MsWordX },
            { MimeTypes.MsPowerPoint, FileExtensions.MsPowerPoint },
            { MimeTypes.MsPowerPointX, FileExtensions.MsPowerPointX },
            { MimeTypes.MsExcel, FileExtensions.MsExcel },
            { MimeTypes.MsExcelX, FileExtensions.MsExcelX },
            { MimeTypes.OpenDocumentText, FileExtensions.OpenDocumentText },
            { MimeTypes.OpenDocumentSpreadsheet, FileExtensions.OpenDocumentSpreadsheet },
            { MimeTypes.OpenDocumentPresentation, FileExtensions.OpenDocumentPresentation },
            { MimeTypes.ElectronicPublicationZip, FileExtensions.ElectronicPublicationZip },
            { MimeTypes.AudioAAC, FileExtensions.AudioAAC },
            { MimeTypes.AudioMP3, FileExtensions.AudioMP3 },
            { MimeTypes.AudioWaveform, FileExtensions.AudioWaveform },
            { MimeTypes.AudioOGG, FileExtensions.AudioOGG },
            { MimeTypes.AudioOpus, FileExtensions.AudioOpus },
            { MimeTypes.AudioWEBM, FileExtensions.AudioWEBM },
            { MimeTypes.VideoMP4, FileExtensions.VideoMP4 },
            { MimeTypes.VideoMPEG, FileExtensions.VideoMPEG },
            { MimeTypes.VideoOGG, FileExtensions.VideoOGG },
            { MimeTypes.VideoOGGGeneric, FileExtensions.VideoOGGGeneric },
            { MimeTypes.VideoWEBM, FileExtensions.VideoWEBM },
            { MimeTypes.ArchiveTar, FileExtensions.ArchiveTar },
            { MimeTypes.ArchiveGzip, FileExtensions.ArchiveGzip },
            { MimeTypes.ArchiveZip, FileExtensions.ArchiveZip },
            { MimeTypes.ArchiveRar, FileExtensions.ArchiveRar },
            { MimeTypes.Archive7Zip, FileExtensions.Archive7Zip },
        };

    /// <summary>
    /// 根据文件扩展名获取对应的 MIME 类型。
    /// </summary>
    /// <param name="extension">文件扩展名（可不带前导点）。</param>
    /// <returns>对应的 MIME 类型，若未找到则返回空字符串。</returns>
    [KernelFunction, Description("根据文件扩展名获取对应的 MIME 类型。")]
    public static string MimeType(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        return s_extensionToMimeType.TryGetValue(extension, out var result) ? result : string.Empty;
    }

    /// <summary>
    /// 根据 MIME 类型获取对应的文件扩展名。
    /// </summary>
    /// <param name="mimeType">MIME 类型。</param>
    /// <returns>对应的文件扩展名（含前导点），若未找到则返回空字符串。</returns>
    [KernelFunction, Description("根据 MIME 类型获取对应的文件扩展名。")]
    public static string Extension(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            return string.Empty;
        }

        return s_mimeTypeToExtension.TryGetValue(mimeType, out var result) ? result : string.Empty;
    }
}
