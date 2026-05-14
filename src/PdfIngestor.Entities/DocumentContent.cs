namespace PdfIngestor.Entities;

public class DocumentContent
{
    public int Id { get; set; }
    
    public int DocumentId { get; init; }

    public Document Document { get; set; } = null!;
    
    public required string ExtractedText { get; init; }
    
    public required int PageCount { get; init; }
    
    public required int TextLength { get; init; }
    
    public required DateTime CreatedAt { get; init; }
}