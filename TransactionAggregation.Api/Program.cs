using Npgsql;   
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using RabbitMQ.Client;
using TransactionAggregation.Persistence;
using TransactionAggregation.Persistence.Repositories;
using System.Text.Json.Serialization;
using TransactionAggregation.Persistence.Domain;
using TransactionAggregation.Contracts;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres")));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});      
 

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();       
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    await dbContext.Database.MigrateAsync();
}



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



    app.MapGet(
    "/api/transactions/{id:guid}",
    async (
        Guid id,
        ITransactionRepository repository,
        CancellationToken cancellationToken) =>
    {
        var transaction = await repository.GetByIdAsync(
            id,
            cancellationToken);

        return transaction is null
            ? Results.NotFound()
            : Results.Ok(transaction);
    });
    app.MapGet(
    "/api/transactions",
    async (
        ITransactionRepository repository,
        CancellationToken cancellationToken) =>
    {
        var transactions = await repository.GetAllAsync(
            cancellationToken);

        return Results.Ok(transactions);
    });
    app.MapGet(
    "/api/transactions/customer/{customerId:guid}/summary",
    async (
        Guid customerId,
        ITransactionRepository repository,
        CancellationToken cancellationToken) =>
    {
        var summary = await repository.GetCustomerSummaryAsync(
            customerId,
            cancellationToken);

        return Results.Ok(summary);
    });
    app.MapGet(
    "/api/transactions/customer/{customerId:guid}/by-payment-method",
    async (
        Guid customerId,
        ITransactionRepository repository,
        CancellationToken cancellationToken) =>
    {
        var summary = await repository.GetPaymentMethodSummaryAsync(
            customerId,
            cancellationToken);

        return Results.Ok(summary);
    });

    app.MapGet(
    "/api/transactions/customer/{customerId:guid}/by-direction",
    async (
        Guid customerId,
        ITransactionRepository repository,
        CancellationToken cancellationToken) =>
    {
        var summary = await repository.GetTransactionDirectionSummaryAsync(
            customerId,
            cancellationToken);

        return Results.Ok(summary);
    });
  app.MapGet(
    "/api/transactions/customer/{customerId:guid}",
    async (
        Guid customerId,
        DateTime? from,
        DateTime? to,
        PaymentMethod? paymentMethod,
        TransactionDirection? direction,
        ITransactionRepository repository,
        CancellationToken cancellationToken) =>
    {
        var transactions = await repository.GetByCustomerAsync(
            customerId,
            from,
            to,
            paymentMethod,
            direction,
            cancellationToken);

        return Results.Ok(transactions);
    });
app.Run();

