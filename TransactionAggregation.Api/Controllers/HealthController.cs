using Microsoft.AspNetCore.Mvc;
using Npgsql;
using RabbitMQ.Client;

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

}
