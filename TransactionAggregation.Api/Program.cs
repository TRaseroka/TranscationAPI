using Npgsql;   
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTPS is handled outside the container for now.
// app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild",
    "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5)
        .Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
        .ToArray();

    return forecast;
})
.WithName("GetWeatherForecast");


app.MapGet("/health/postgres", async (IConfiguration configuration) =>
{
    var connectionString =
        configuration.GetConnectionString("Postgres");

    await using var connection = new NpgsqlConnection(connectionString);

    await connection.OpenAsync();

    await using var command = new NpgsqlCommand(
        "SELECT 1",
        connection);

    var result = await command.ExecuteScalarAsync();

    return Results.Ok(new
    {
        database = "PostgreSQL",
        status = result?.ToString() == "1" ? "Healthy" : "Unhealthy"
    });
});

app.MapGet("/health/rabbitmq", async (IConfiguration configuration) =>
{
    var host = configuration["RabbitMQ:Host"];
    var port = configuration.GetValue<int>("RabbitMQ:Port");
    var username = configuration["RabbitMQ:Username"];
    var password = configuration["RabbitMQ:Password"];

    var factory = new ConnectionFactory
    {
        HostName = host,
        Port = port,
        UserName = username,
        Password = password
    };

    await using var connection = await factory.CreateConnectionAsync();

    return Results.Ok(new
    {
        service = "RabbitMQ",
        status = connection.IsOpen ? "Healthy" : "Unhealthy"
    });
});
app.Run();

record WeatherForecast(
    DateOnly Date,
    int TemperatureC,
    string? Summary)
{
    public int TemperatureF =>
        32 + (int)(TemperatureC / 0.5556);
}