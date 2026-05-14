using PdfIngestor.App.Dto;

namespace PdfIngestor.App.Contract;

public interface IExtractTextHandler
{
    Task Handle(ExtractTextCommand command, CancellationToken cancellationToken);
}