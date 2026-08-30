using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using TransactionAggregation.Application.Interfaces;
using TransactionAggregation.Application.Services;
using TransactionAggregation.Persistence;
using TransactionAggregation.Persistence.Repositories;
using TransactionAggregation.Application.Mappings;


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
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<TransactionMappingProfile>();
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
app.UseSwagger();
app.UseSwaggerUI();
}

await using (var scope = app.Services.CreateAsyncScope())
{
var dbContext =
scope.ServiceProvider.GetRequiredService<TransactionDbContext>();


await dbContext.Database.MigrateAsync();

}

app.MapControllers();

app.Run();

