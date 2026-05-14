using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using PdfIngestor.App.Configs;
using PdfIngestor.App.Contract;
using PdfIngestor.App.Dto;
using RabbitMQ.Client.Events;

namespace PdfIngestor.App.Services;

public class RabbitMqBrokerService : IBrokerService, IAsyncDisposable
{
    private IConnection? _connection;

    private readonly RabbitMqConfig _config;
    private readonly ILogger<RabbitMqBrokerService> _logger;

    private readonly SemaphoreSlim _channelLock;
    private readonly ConcurrentBag<IChannel> _channels = [];
    private const int PoolSize = 20;
    
    public RabbitMqBrokerService(
        RabbitMqConfig config,
        ILogger<RabbitMqBrokerService> logger)
    {
        _config = config;
        _logger = logger;
        _channelLock = new SemaphoreSlim(PoolSize, PoolSize);
    }
    
    public async Task Initialize(CancellationToken cancellationToken = default)
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

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        
        var topologyChannel = await _connection.CreateChannelAsync(cancellationToken:cancellationToken);
        
        await topologyChannel.QueueDeclareAsync(
            queue: _config.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        
        await topologyChannel.DisposeAsync();
        
        for (var i = 0; i < PoolSize; i++)
        {
            var channel = await CreateChannel(cancellationToken);
            _channels.Add(channel);
        }

        _logger.LogInformation("RabbitMQ initialized with channel pool size: {PoolSize}", PoolSize);        
    }    
    
    public async Task Publish(ExtractTextCommand command, CancellationToken cancellationToken = default)
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("RabbitMQ is not initialized");
        }

        await _channelLock.WaitAsync(cancellationToken);

        IChannel? channel = null;

        try
        {
            if (!_channels.TryTake(out channel))
            {
                channel = await CreateChannel(cancellationToken);
            }

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command));

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _config.QueueName,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Message published for document {DocumentId}", command.FileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message for document {DocumentId}", command.FileId);

            throw;
        }
        finally
        {
            if (channel is not null && channel.IsOpen)
            {
                _channels.Add(channel);
            }
            else if (_connection?.IsOpen == true)
            {
                var newChannel = await CreateChannel(cancellationToken);
                _channels.Add(newChannel);
            }

            _channelLock.Release();            
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        while (_channels.TryTake(out var channel))
        {
            await channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
        
        _channelLock.Dispose();
    }
    
    private async Task<IChannel> CreateChannel(
        CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("Connection is null");
        }

        var channel = await _connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        channel.BasicReturnAsync += OnMessageReturned;
        
        return channel;
    }
    
    private Task OnMessageReturned(
        object sender,
        BasicReturnEventArgs args)
    {
        var body = Encoding.UTF8.GetString(args.Body.ToArray());

        _logger.LogError(
            """
            RabbitMQ returned message.
            Code: {Code}
            Text: {Text}
            RoutingKey: {RoutingKey}
            Message: {Message}
            """,
            args.ReplyCode,
            args.ReplyText,
            args.RoutingKey,
            body);

        return Task.CompletedTask;
    }
}