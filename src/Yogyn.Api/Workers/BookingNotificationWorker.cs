using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Yogyn.Api.Events;
using Yogyn.Api.Services;

namespace Yogyn.Api.Workers;

public class BookingNotificationWorker : BackgroundService
{
    private readonly ILogger<BookingNotificationWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private ServiceBusProcessor? _processor;

    public BookingNotificationWorker(
        ILogger<BookingNotificationWorker> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration["servicebus-connection-string"];
        var queueName = _configuration["ServiceBus:QueueName"];

        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(queueName))
        {
            _logger.LogError("Service Bus configuration missing. Worker will not start.");
            return;
        }

        var client = new ServiceBusClient(connectionString);
        _processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);
        
        _logger.LogInformation("BookingNotificationWorker started and listening for messages");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("BookingNotificationWorker is stopping");
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageBody = args.Message.Body.ToString();
        var eventType = args.Message.Subject;

        _logger.LogInformation("Processing message: {EventType}", eventType);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            switch (eventType)
            {
                case nameof(BookingCreatedEvent):
                    var bookingCreated = JsonSerializer.Deserialize<BookingCreatedEvent>(messageBody);
                    if (bookingCreated != null)
                    {
                        if (bookingCreated.Status == Models.BookingStatus.Confirmed)
                        {
                            await emailService.SendBookingConfirmationAsync(bookingCreated);
                        }
                        else if (bookingCreated.Status == Models.BookingStatus.Pending)
                        {
                            await emailService.SendBookingPendingAsync(bookingCreated);
                        }
                    }
                    break;

                case nameof(BookingApprovedEvent):
                    var bookingApproved = JsonSerializer.Deserialize<BookingApprovedEvent>(messageBody);
                    if (bookingApproved != null)
                    {
                        await emailService.SendBookingApprovedAsync(bookingApproved);
                    }
                    break;

                case nameof(BookingRejectedEvent):
                    var bookingRejected = JsonSerializer.Deserialize<BookingRejectedEvent>(messageBody);
                    if (bookingRejected != null)
                    {
                        await emailService.SendBookingRejectedAsync(bookingRejected);
                    }
                    break;

                case nameof(BookingCancelledEvent):
                    var bookingCancelled = JsonSerializer.Deserialize<BookingCancelledEvent>(messageBody);
                    if (bookingCancelled != null)
                    {
                        await emailService.SendBookingCancelledAsync(bookingCancelled);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventType);
                    break;
            }

            await args.CompleteMessageAsync(args.Message);
            _logger.LogInformation("Message processed successfully: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {EventType}", eventType);
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error in message processor: {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingNotificationWorker is stopping");

        if (_processor != null)
        {
            await _processor.StopProcessingAsync(stoppingToken);
            await _processor.DisposeAsync();
        }

        await base.StopAsync(stoppingToken);
    }
}
