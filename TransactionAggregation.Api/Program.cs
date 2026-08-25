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
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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


app.MapPost(
    "/api/transactions/v1/",
    async (
        Transaction transaction,
        ITransactionRepository repository,
        CancellationToken cancellationToken) =>
    {
        if (transaction.Id == Guid.Empty)
        {
            transaction.Id = Guid.NewGuid();
        }

        await repository.AddAsync(
            transaction,
            cancellationToken);

        await repository.SaveChangesAsync(
            cancellationToken);

        return Results.Created(
            $"/api/transactions/{transaction.Id}",
            transaction);
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

app.MapPost(
    "/api/transactions",
    async (
        Transaction transaction,
        ITransactionRepository repository,
        CancellationToken cancellationToken) =>
    {
        if (transaction.Id == Guid.Empty)
        {
            transaction.Id = Guid.NewGuid();
        }

        await repository.AddAsync(
            transaction,
            cancellationToken);

        await repository.SaveChangesAsync(
            cancellationToken);

        return Results.Created(
            $"/api/transactions/{transaction.Id}",
            transaction);
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
    "/api/transactions/customer/{customerId:guid}/v1/",
    async (
        Guid customerId,
        ITransactionRepository repository,
        CancellationToken cancellationToken) =>
    {
        var transactions = await repository.GetByCustomerIdAsync(
            customerId,
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

