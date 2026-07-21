using BGA.Contracts.Bookings;
using BGA.Events.Application.Settings;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BGA.Events.Infrastructure.Messaging;

public sealed class KafkaTopicInitializerHostedService(
    IOptions<KafkaOptions> options,
    ILogger<KafkaTopicInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = new AdminClientConfig { BootstrapServers = options.Value.BootstrapServers, SocketTimeoutMs = 5000 };

            using var adminClient = new AdminClientBuilder(config).Build();
            await adminClient.CreateTopicsAsync([new TopicSpecification { Name = BookingTopics.BookingConfirmed, NumPartitions = 1, ReplicationFactor = 1 }]);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            logger.LogInformation("Kafka topic {Topic} already exists", BookingTopics.BookingConfirmed);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Kafka topic {Topic} was not created at startup", BookingTopics.BookingConfirmed);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
