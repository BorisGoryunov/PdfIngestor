using System.Text.Json;
using Microsoft.Extensions.Logging;
using PdfIngestor.App.Contract;
using PdfIngestor.App.Dto;
using PdfIngestor.Entities;
using PdfIngestor.Entities.Enums;
using PdfIngestor.Persistence;

namespace PdfIngestor.App.Services;

public class UploadDocumentService : IUploadDocumentService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<UploadDocumentService> _logger;
    private readonly AppDbContext _appDbContext;

    public UploadDocumentService(IFileStorageService fileStorageService,
        ILogger<UploadDocumentService> logger,
        AppDbContext  appDbContext)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
        _appDbContext = appDbContext;
    }

    public async Task<Result<DocumentUploadResponse>> Execute(Stream fileStream, string fileName, long fileSize, CancellationToken cancellationToken)
    {
        var document = new Document
        {
            FileId = Guid.NewGuid(),
            FileName = fileName,
            FileSize = fileSize,
            UploadedAt = DateTime.UtcNow
        };

        try
        {
            document.FilePath = await _fileStorageService.SaveFile(fileStream, document.FileId);
            
            await _appDbContext.AddAsync(document, cancellationToken);

            var outboxMessage = new OutboxMessage
            {
                Payload = JsonSerializer.Serialize(
                    new ExtractTextCommand
                    {
                        FileId = document.FileId,
                        FileName = document.FileName,
                        FilePath = document.FilePath
                    }),
                CreatedAt = DateTime.UtcNow
            };
            
            await _appDbContext.AddAsync(outboxMessage, cancellationToken);
            
            document.Status = DocumentStatus.Queued;
            document.QueuedAt = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;
            
            await _appDbContext.SaveChangesAsync(cancellationToken);
            
            return Result<DocumentUploadResponse>.Success(new DocumentUploadResponse(
                document.Id,
                document.FileName,
                document.Status,
                document.UploadedAt
            ));        
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document");
            
            if (!string.IsNullOrWhiteSpace(document.FilePath))
            {
                _fileStorageService.DeleteFile(document.FilePath);
            }
            
            throw;            
        }
    }
}