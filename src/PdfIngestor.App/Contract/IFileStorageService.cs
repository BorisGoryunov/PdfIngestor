using System;
using System.IO;
using System.Threading.Tasks;

namespace PdfIngestor.App.Contract;

public interface IFileStorageService
{
    Task<string> SaveFile(Stream fileStream, Guid documentId);
    
    void DeleteFile(string filePath);
}