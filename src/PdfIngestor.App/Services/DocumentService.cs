using Microsoft.EntityFrameworkCore;
using PdfIngestor.App.Dto;
using PdfIngestor.Entities;
using PdfIngestor.Persistence;

namespace PdfIngestor.App.Services;

public class DocumentService
{
    private readonly AppDbContext _appDbContext;

    public DocumentService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Result<IReadOnlyList<DocumentItem>>> GetDocuments(int offset, int limit, CancellationToken cancellationToken)
    {
        var data = await _appDbContext.Set<Document>()
            .OrderBy(x => x.Id)
            .Skip(offset)
            .Take(limit)
            .Select(x => new DocumentItem
            {
                Id = x.Id,
                FileId = x.FileId,
                FileName = x.FileName,
                UploadedAt = x.UploadedAt,
                FileSize = x.FileSize,
                ProcessedAt = x.ProcessedAt,
                ProcessingStartedAt = x.ProcessingStartedAt,
                UpdatedAt = x.UpdatedAt,
                FilePath = x.FilePath,
                QueuedAt = x.QueuedAt,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);

        var result = Result<IReadOnlyList<DocumentItem>>.Success(data);

        return result;
    }

    public async Task<Result<string>> GetContent(int documentId, CancellationToken cancellationToken)
    {
        var data = await _appDbContext.Set<DocumentContent>()
            .Where(x => x.DocumentId == documentId)
            .Select(x => x.ExtractedText)
            .FirstOrDefaultAsync(cancellationToken);

        var result = Result<string>.Success(data ?? string.Empty);
        return result;
    }
}