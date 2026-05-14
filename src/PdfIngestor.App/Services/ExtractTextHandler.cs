using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PdfIngestor.App.Contract;
using PdfIngestor.App.Dto;
using PdfIngestor.Entities;
using PdfIngestor.Entities.Enums;
using PdfIngestor.Persistence;

namespace PdfIngestor.App.Services;

public sealed class ExtractTextHandler : IExtractTextHandler
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<ExtractTextHandler> _logger;
    private readonly IPdfTextExtractor _pdfTextExtractor;

    public ExtractTextHandler(AppDbContext appDbContext,
        ILogger<ExtractTextHandler> logger,
        IPdfTextExtractor pdfTextExtractor)
    {
        _appDbContext = appDbContext;
        _logger = logger;
        _pdfTextExtractor = pdfTextExtractor;
    }

    public async Task Handle(ExtractTextCommand command, CancellationToken cancellationToken)
    {
        var document = await _appDbContext.Set<Document>()
            .Where(x => x.FileId == command.FileId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            _logger.LogWarning("Document not found: {DocumentId}", command.FileId);
            return;
        }

        if (document.Status == DocumentStatus.Processed)
        {
            _logger.LogInformation("Document already completed: {DocumentId}", command.FileId);
            return;
        }

        document.Status = DocumentStatus.Processing;
        document.ProcessingStartedAt = DateTime.UtcNow;
        
        await _appDbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await _pdfTextExtractor.Extract(command.FilePath, cancellationToken);

            var content = new DocumentContent
            {
                DocumentId = document.Id,
                ExtractedText = result.Text,
                PageCount = result.PageCount,
                TextLength = result.TextLength,
                CreatedAt = DateTime.UtcNow
            };

            await _appDbContext.Set<DocumentContent>()
                .AddAsync(content, cancellationToken);

            document.Status = DocumentStatus.Processed;
            document.ProcessedAt = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;

            await _appDbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document processed successfully: {DocumentId}", document.Id);

        }
        catch (Exception ex)
        {
            document.Status = DocumentStatus.Failed;
            document.UpdatedAt = DateTime.UtcNow;

            await _appDbContext.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Failed processing document {DocumentId}",
                document.Id);

            throw;                                    
        }
    }
}