using System.Text.Json;
using Confluent.Kafka;
using Fundoo.User.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fundoo.User.Infrastructure.Messaging;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(
        IConfiguration configuration,
        ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;

        var bootstrapServers =
            configuration["Kafka:BootstrapServers"];

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            throw new InvalidOperationException(
                "Kafka bootstrap servers are not configured.");
        }

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers, //Address of Kafka broker
            Acks = Acks.All,
            EnableIdempotence = true //Prevents duplicate writes caused by producer retries within Kafka's idempotent-producer guarantees
        };

        _producer = new ProducerBuilder<Null, string>(
            producerConfig).Build();
    }

    public async Task PublishAsync<TEvent>(
        string topic,
        TEvent message,
        CancellationToken cancellationToken = default)
    {
        //Kafka stores bytes, not C# objects. Serialization converts our UserRegisteredEvent into JSON before publishing.
        var serializedMessage =
            JsonSerializer.Serialize(message);

        var kafkaMessage = new Message<Null, string>
        {
            Value = serializedMessage
        };
        //This sends the event to Kafka and asynchronously waits for the delivery result.
        var deliveryResult = await _producer.ProduceAsync(
            topic,
            kafkaMessage,
            cancellationToken);

        _logger.LogInformation(
            "Event published to Kafka. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
            deliveryResult.Topic,
            deliveryResult.Partition.Value,
            deliveryResult.Offset.Value);
        //These tell us where Kafka stored the event.
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}