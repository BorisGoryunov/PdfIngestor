using Microsoft.Extensions.Logging;
using PdfIngestor.App.Contract;

namespace PdfIngestor.App.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath = Path.Combine(AppContext.BaseDirectory, "Storage", "PdfFiles");
    
    public LocalFileStorageService(ILogger<LocalFileStorageService> logger)
    {
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            logger.LogInformation("Created storage directory {Directory}",  _storagePath);
        }
    }
    
    public async Task<string> SaveFile(Stream fileStream, Guid documentId)
    {
        var fileName = $"{documentId}.pdf";
        var filePath = Path.Combine(_storagePath, fileName);

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(stream);

        return filePath;
    }
    
    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}