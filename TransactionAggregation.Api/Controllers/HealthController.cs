using Microsoft.AspNetCore.Mvc;
using Npgsql;
using RabbitMQ.Client;
using Confluent.Kafka;

namespace TransactionAggregation.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
private readonly IConfiguration _configuration;

public HealthController(IConfiguration configuration)
{
    _configuration = configuration;
}

[HttpGet("postgres")]
public async Task<IActionResult> Postgres(
    CancellationToken cancellationToken)
{
    var connectionString =
        _configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException(
            "Postgres connection string is not configured.");

    await using var connection =
        new NpgsqlConnection(connectionString);

    await connection.OpenAsync(cancellationToken);

    await using var command =
        new NpgsqlCommand("SELECT 1", connection);

    var result =
        await command.ExecuteScalarAsync(cancellationToken);

    return Ok(new
    {
        database = "PostgreSQL",
        status = result?.ToString() == "1"
            ? "Healthy"
            : "Unhealthy"
    });
}

[HttpGet("rabbitmq")]
public async Task<IActionResult> RabbitMq()
{
    var host =
        _configuration["RabbitMQ:Host"]
        ?? throw new InvalidOperationException(
            "RabbitMQ:Host is not configured.");

    var port =
        _configuration.GetValue<int>("RabbitMQ:Port");

    var username =
        _configuration["RabbitMQ:Username"]
        ?? throw new InvalidOperationException(
            "RabbitMQ:Username is not configured.");

    var password =
        _configuration["RabbitMQ:Password"]
        ?? throw new InvalidOperationException(
            "RabbitMQ:Password is not configured.");

    var factory = new ConnectionFactory
    {
        HostName = host,
        Port = port,
        UserName = username,
        Password = password
    };

    await using var connection =
        await factory.CreateConnectionAsync();

    return Ok(new
    {
        service = "RabbitMQ",
        status = connection.IsOpen
            ? "Healthy"
            : "Unhealthy"
    });
}
[HttpGet("kafka")]
public async Task<IActionResult> Kafka(
    CancellationToken cancellationToken)
{
    try
    {
        var bootstrapServers =
            _configuration["Kafka:BootstrapServers"];

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            return StatusCode(503, new
            {
                service = "Kafka",
                status = "Unhealthy",
                error = "Kafka:BootstrapServers is not configured."
            });
        }

        var topic =
            _configuration["Kafka:Topic"]
            ?? "transactions";

        var config = new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        };

        using var adminClient =
            new AdminClientBuilder(config).Build();

        var metadata = await Task.Run(
            () => adminClient.GetMetadata(
                topic,
                TimeSpan.FromSeconds(5)),
            cancellationToken);

        var topicMetadata =
            metadata.Topics.FirstOrDefault();

        if (topicMetadata == null)
        {
            return StatusCode(503, new
            {
                service = "Kafka",
                status = "Unhealthy",
                topic,
                error = "Topic was not found."
            });
        }

        if (topicMetadata.Error.IsError)
        {
            return StatusCode(503, new
            {
                service = "Kafka",
                status = "Unhealthy",
                topic,
                error = topicMetadata.Error.Reason
            });
        }

        return Ok(new
        {
            service = "Kafka",
            status = "Healthy",
            brokerCount = metadata.Brokers.Count,
            topic,
            partitionCount = topicMetadata.Partitions.Count
        });
    }
    catch (Exception ex)
    {
        return StatusCode(503, new
        {
            service = "Kafka",
            status = "Unhealthy",
            error = ex.Message
        });
    }
}
}
