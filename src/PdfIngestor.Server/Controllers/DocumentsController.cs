using Microsoft.AspNetCore.Mvc;
using PdfIngestor.App.Contract;
using PdfIngestor.App.Services;

namespace PdfIngestor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IUploadDocumentService _uploadDocumentService;
    private readonly DocumentService _documentService;

    public DocumentsController(IUploadDocumentService uploadDocumentService, DocumentService documentService)
    {
        _uploadDocumentService = uploadDocumentService;
        _documentService = documentService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        await using var fileStream = file.OpenReadStream();
        var result = await _uploadDocumentService.Execute(fileStream, file.FileName, file.Length, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments(int offset, int limit, CancellationToken cancellationToken)
    {
        var result = await _documentService.GetDocuments(offset, limit, cancellationToken); 
        return Ok(result);
    }

    [HttpGet("{documentId:int}/content")]
    public async Task<IActionResult> GetContent(int documentId, CancellationToken cancellationToken)
    {
        var result = await _documentService.GetContent(documentId, cancellationToken);
        return Ok(result);
    }
}