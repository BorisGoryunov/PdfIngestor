using System.IO;
using System.Threading.Tasks;
using PdfIngestor.App.Dto;

namespace PdfIngestor.App.Contract;

public interface IUploadDocumentService
{
    Task<Result<DocumentUploadResponse>> Execute(Stream fileStream, string fileName, long fileSize, CancellationToken cancellationToken);
}