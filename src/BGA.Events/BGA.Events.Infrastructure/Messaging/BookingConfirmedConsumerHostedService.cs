using BGA.Contracts.Bookings;
using BGA.Events.Application.Services.Interfaces;
using BGA.Events.Application.Settings;
using BGA.Events.Domain.Exceptions;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BGA.Events.Infrastructure.Messaging;

public sealed class BookingConfirmedConsumerHostedService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingConfirmedConsumerHostedService> _logger;

    public BookingConfirmedConsumerHostedService(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<BookingConfirmedConsumerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = options.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(BookingTopics.BookingConfirmed);
        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;
            try
            {
                result = _consumer.Consume(stoppingToken);
                var message = JsonSerializer.Deserialize<BookingConfirmed>(result.Message.Value)
                    ?? throw new JsonException("BookingConfirmed payload is null.");

                await using var scope = _scopeFactory.CreateAsyncScope();
                var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

                var isReserved = await eventService.TryReserveSeatsAsync(message.EventId, message.SeatsCount, stoppingToken);
                if (!isReserved)
                {
                    _logger.LogWarning("Booking {BookingId} skipped: not enough seats for event {EventId}", message.BookingId, message.EventId);
                }

                _consumer.Commit(result);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (JsonException ex) when (result is not null)
            {
                _logger.LogWarning(ex, "Invalid booking confirmation payload at {Offset}", result.TopicPartitionOffset);
                _consumer.Commit(result);
            }
            catch (NotFoundException ex) when (result is not null)
            {
                _logger.LogWarning(ex, "Booking confirmation skipped: event not found at {Offset}", result.TopicPartitionOffset);
                _consumer.Commit(result);
            }
            catch (ValidationException ex) when (result is not null)
            {
                _logger.LogWarning(ex, "Booking confirmation skipped due to invalid data at {Offset}",
                    result.TopicPartitionOffset);
                _consumer.Commit(result);
            }
            catch (Exception ex) when (result is not null)
            {
                _logger.LogError(ex, "Booking confirmation processing failed at {Offset}", result.TopicPartitionOffset);
                _consumer.Seek(result.TopicPartitionOffset);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}
