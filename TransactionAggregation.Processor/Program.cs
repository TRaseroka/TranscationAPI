using Microsoft.EntityFrameworkCore;
using TransactionAggregation.Persistence;
using TransactionAggregation.Persistence.Repositories;
using TransactionAggregation.Processor;
using TransactionAggregation.Processor.Messaging.RabbitMQ;

var builder = Host.CreateApplicationBuilder(args);

var rabbitMqOptions = new RabbitMqOptions
{
    Host = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq",
    Port = int.TryParse(
        builder.Configuration["RabbitMQ:Port"],
        out var port)
        ? port
        : 5672,
    Username = builder.Configuration["RabbitMQ:Username"] ?? string.Empty,
    Password = builder.Configuration["RabbitMQ:Password"] ?? string.Empty,
    QueueName = builder.Configuration["RabbitMQ:QueueName"] ?? "transactions"
};

builder.Services.AddSingleton(rabbitMqOptions);

builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();