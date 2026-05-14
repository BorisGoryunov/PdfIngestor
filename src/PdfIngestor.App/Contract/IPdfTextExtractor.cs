using PdfIngestor.App.Dto;

namespace PdfIngestor.App.Contract;

public interface IPdfTextExtractor
{
    Task<PdfTextExtractResult> Extract(string filePath, CancellationToken cancellationToken = default);
}