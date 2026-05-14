using PdfIngestor.Entities.Enums;

namespace PdfIngestor.App.Dto;

public class DocumentItem
{
    public int Id { get; set; }
    
    public required Guid FileId { get; init; }
    
    public required string FileName { get; init; }
    
    public required long FileSize { get; init; }

    public string FilePath { get; set; } = null!;
    
    public DocumentStatus Status { get; set; }
    
    public DateTime UploadedAt { get; set; }
    
    public DateTime? QueuedAt { get; set; }
    
    public DateTime? ProcessingStartedAt { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}