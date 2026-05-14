namespace PdfIngestor.Entities;

public class OutboxMessage
{
    public int Id { get; set; }
    
    public required string Payload { get; init; }
    
    public required DateTime CreatedAt { get; init; }
    
    public DateTime? ProcessedAt { get; set; }

    public string? Error { get; set; }

    public int RetryCount { get; set; }
}