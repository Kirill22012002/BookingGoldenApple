using BGA.Bookings.Application.Messaging;
using BGA.Bookings.Application.Settings;
using BGA.Contracts.Bookings;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BGA.Bookings.Infrastructure.Messaging;

public sealed class KafkaBookingConfirmedPublisher : IBookingConfirmedPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaBookingConfirmedPublisher> _logger;

    public KafkaBookingConfirmedPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaBookingConfirmedPublisher> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken = default)
    {
        var deliveryResult = await _producer.ProduceAsync(
            BookingTopics.BookingConfirmed,
            new Message<string, string>
            {
                Key = message.EventId.ToString(),
                Value = JsonSerializer.Serialize(message)
            },
            cancellationToken);

        _logger.LogInformation(
            "Published booking confirmation {BookingId} to {TopicPartitionOffset}",
            message.BookingId,
            deliveryResult.TopicPartitionOffset);
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
