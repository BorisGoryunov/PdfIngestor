using System.Text;
using PdfIngestor.App.Contract;
using PdfIngestor.App.Dto;
using UglyToad.PdfPig;

namespace PdfIngestor.App.Services;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public async Task<PdfTextExtractResult> Extract(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("PDF file not found", filePath);
        }

        var textBuilder = new StringBuilder();

        using var document = PdfDocument.Open(filePath);

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            textBuilder.AppendLine(page.Text);
        }

        var text = textBuilder.ToString();

        var result = new PdfTextExtractResult(
            text,
            document.NumberOfPages,
            text.Length);
        
        return await Task.FromResult(result);
    }
}