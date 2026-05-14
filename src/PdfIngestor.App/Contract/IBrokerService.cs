using System.Threading.Tasks;
using PdfIngestor.App.Dto;

namespace PdfIngestor.App.Contract;

public interface IBrokerService
{
    Task Publish(ExtractTextCommand command, CancellationToken cancellationToken);
}