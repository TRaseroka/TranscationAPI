using Microsoft.EntityFrameworkCore;
using TransactionAggregation.Persistence;
using TransactionAggregation.Persistence.Repositories;
using TransactionAggregation.Processor;
using TransactionAggregation.Processor.Messaging.RabbitMQ;
using TransactionAggregation.Application.Interfaces;
using TransactionAggregation.Application.Services;
using TransactionAggregation.Application.Mappings;
using TransactionAggregation.Processor.Kafka;
using TransactionAggregation.Processor.Messaging;

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
builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITransactionMessageHandler, TransactionMessageHandler>();
builder.Services.AddHostedService<KafkaTransactionConsumer>();
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<TransactionMappingProfile>();
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();