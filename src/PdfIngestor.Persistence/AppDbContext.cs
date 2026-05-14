using Microsoft.EntityFrameworkCore;
using PdfIngestor.Entities;

namespace PdfIngestor.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var document = modelBuilder.Entity<Document>();
        document.ToTable(nameof(Document));

        document.Property(x => x.FileName)
            .HasMaxLength(150);

        document.Property(x => x.FilePath)
            .HasMaxLength(512);
        
        document.HasIndex(x=>x.FileId)
            .IsUnique();
        
        var documentContent = modelBuilder.Entity<DocumentContent>();
        documentContent.ToTable(nameof(DocumentContent));
        
        var outboxMessage = modelBuilder.Entity<OutboxMessage>();
        outboxMessage.ToTable(nameof(OutboxMessage));
        outboxMessage.Property(x=>x.Payload)
            .HasColumnType("jsonb");
        outboxMessage.HasIndex(x => x.ProcessedAt);
        outboxMessage.HasIndex(x => x.CreatedAt);
        
    }
}