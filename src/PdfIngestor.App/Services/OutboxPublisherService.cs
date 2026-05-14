using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PdfIngestor.App.Contract;
using PdfIngestor.App.Dto;
using PdfIngestor.Entities;
using PdfIngestor.Persistence;

namespace PdfIngestor.App.Services;

public sealed class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processing failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessMessages(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var broker = scope.ServiceProvider.GetRequiredService<IBrokerService>();

        var messages = await db.Set<OutboxMessage>()
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var command = JsonSerializer.Deserialize<ExtractTextCommand>(message.Payload);

                if (command is null)
                {
                    continue;
                }

                await broker.Publish(command, cancellationToken);
                
                message.ProcessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                _logger.LogError(
                    ex,
                    "Failed processing outbox message {MessageId}",
                    message.Id);
            }
        }
    }
}