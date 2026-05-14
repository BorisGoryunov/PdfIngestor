namespace PdfIngestor.App.Dto;

public class ExtractTextCommand
{
    public required Guid FileId { get; init; }
    
    public required string FilePath { get; init; }
    
    public required string FileName { get; init; }
}