using Microsoft.Extensions.DependencyInjection;
using PdfIngestor.App.Contract;
using PdfIngestor.App.Services;

namespace PdfIngestor.App;

public static class Installer
{
    public static void AddApp(this IServiceCollection services)
    {
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IUploadDocumentService, UploadDocumentService>();
        
        services.AddSingleton<RabbitMqBrokerService>();

        services.AddSingleton<IBrokerService>(sp =>
        {
            var service = sp.GetRequiredService<RabbitMqBrokerService>();
            service.Initialize().GetAwaiter().GetResult();
            return service;
        });

        services.AddHostedService<OutboxPublisherService>();
        services.AddScoped<IExtractTextHandler, ExtractTextHandler>();
        services.AddHostedService<RabbitMqConsumerService>();
        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
        services.AddScoped<DocumentService>();
    }
}