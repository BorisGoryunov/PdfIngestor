namespace PdfIngestor.App.Dto;

public sealed record PdfTextExtractResult(
    string Text,
    int PageCount,
    int TextLength);