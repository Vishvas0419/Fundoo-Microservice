
using System.Text.Json;
using Confluent.Kafka;
using Fundoo.Contracts.Events;
using Fundoo.Notification.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fundoo.Notification.Infrastructure.Messaging;

public sealed class UserRegisteredConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<UserRegisteredConsumer> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers =
                _configuration["Kafka:BootstrapServers"],

            GroupId =
                _configuration["Kafka:GroupId"],

            AutoOffsetReset = AutoOffsetReset.Latest,

            EnableAutoCommit = false,

            EnableAutoOffsetStore = false
        };

        var topic =
            _configuration["Kafka:UserRegisteredTopic"]
            ?? throw new InvalidOperationException(
                "Kafka topic is not configured.");

        using var consumer =
            new ConsumerBuilder<Ignore, string>(
                consumerConfig).Build();

        consumer.Subscribe(topic);

        _logger.LogInformation(
            "Kafka consumer started. Topic: {Topic}",
            topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<Ignore, string> result;

                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(
                        ex,
                        "Kafka consumption error");

                    continue;
                }

                try
                {
                    var registeredEvent =
                        JsonSerializer.Deserialize<UserRegisteredEvent>(
                            result.Message.Value)
                        ?? throw new InvalidOperationException(
                            "Invalid UserRegistered event.");

                    using var scope =
                        _scopeFactory.CreateScope();

                    var notificationService =
                        scope.ServiceProvider
                            .GetRequiredService<INotificationService>();

                    await notificationService.SendEmailAsync(
                        registeredEvent.Email,
                        $"Welcome {registeredEvent.FirstName}",
                        $"Hello {registeredEvent.FirstName}, welcome to Fundoo!",
                        stoppingToken);

                    // Commit only after successful email processing.
                    consumer.Commit(result);

                    _logger.LogInformation(
                        "Welcome email processed. EventId: {EventId}, UserId: {UserId}",
                        registeredEvent.EventId,
                        registeredEvent.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to process UserRegistered event. Partition: {Partition}, Offset: {Offset}",
                        result.Partition.Value,
                        result.Offset.Value);

                    // Do not commit a failed message.
                    // Stop this consumer so the message can be
                    // processed again when the service restarts.
                    throw;
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}
