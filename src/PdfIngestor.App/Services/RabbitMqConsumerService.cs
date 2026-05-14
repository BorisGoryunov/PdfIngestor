using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PdfIngestor.App.Configs;
using PdfIngestor.App.Contract;
using PdfIngestor.App.Dto;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PdfIngestor.App.Services;

public sealed class RabbitMqConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConfig _config;
    private readonly ILogger<RabbitMqConsumerService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumerService(
        IServiceScopeFactory scopeFactory,
        RabbitMqConfig config,
        ILogger<RabbitMqConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    public override async Task StartAsync(
        CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config.HostName,
            Port = _config.Port,
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = await factory.CreateConnectionAsync(
            cancellationToken);

        _channel = await _connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: _config.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false,
            cancellationToken: cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
        {
            throw new InvalidOperationException("Channel not initialized");
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += OnMessage;

        await _channel.BasicConsumeAsync(
            queue: _config.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("RabbitMQ consumer started");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnMessage(object sender, BasicDeliverEventArgs args)
    {
        try
        {
            var body = Encoding.UTF8.GetString(args.Body.ToArray());

            var command = JsonSerializer.Deserialize<ExtractTextCommand>(body);

            if (command is null)
            {
                await _channel!.BasicNackAsync(
                    args.DeliveryTag,
                    multiple: false,
                    requeue: false);

                return;
            }

            using var scope = _scopeFactory.CreateScope();

            var handler = scope.ServiceProvider.GetRequiredService<IExtractTextHandler>();

            await handler.Handle(command, CancellationToken.None);

            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing RabbitMQ message");

            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}