using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Yogyn.Api.Events;

namespace Yogyn.Api.Services;

public class ServiceBusService : IMessageBusService, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusService> _logger;

    public ServiceBusService(
        IConfiguration configuration,
        ILogger<ServiceBusService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration["servicebus-connection-string"]
            ?? throw new InvalidOperationException("ServiceBus connection string not found");
        
        var queueName = configuration["ServiceBus:QueueName"]
            ?? throw new InvalidOperationException("ServiceBus queue name not found");

        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(queueName);
        
        _logger.LogInformation("ServiceBusService initialized for queue: {QueueName}", queueName);
    }

    public async Task PublishAsync<T>(T eventMessage) where T : IEvent
    {
        try
        {
            var json = JsonSerializer.Serialize(eventMessage);
            
            var message = new ServiceBusMessage(json)
            {
                ContentType = "application/json",
                Subject = typeof(T).Name,
                MessageId = Guid.NewGuid().ToString()
            };

            await _sender.SendMessageAsync(message);
            
            _logger.LogInformation(
                "Published {EventType} for booking {BookingId}", 
                typeof(T).Name, 
                eventMessage.BookingId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Failed to publish {EventType} for booking {BookingId}", 
                typeof(T).Name, 
                eventMessage.BookingId
            );
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
