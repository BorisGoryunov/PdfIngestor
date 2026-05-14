using System;
using PdfIngestor.Entities.Enums;

namespace PdfIngestor.App.Dto;

public record DocumentUploadResponse(
    int Id,
    string FileName,
    DocumentStatus Status,
    DateTime UploadedAt
);