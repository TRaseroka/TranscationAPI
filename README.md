# Transaction Aggregation API

A comprehensive .NET solution for processing, storing, and aggregating financial transactions from multiple sources with real-time message processing capabilities.

## Overview

The Transaction Aggregation system is built on a microservices architecture that ingests transaction data from various sources (XML, JSON), processes them through a message broker, and provides a RESTful API for querying aggregated transaction information by customer, payment method, and transaction direction.

## Architecture

The solution follows a clean, layered architecture pattern:

```
┌─────────────────────────────────────────────┐
│         TransactionAggregation.Api          │  REST API Layer
│    (HTTP Controllers, Exception Handling)   │
└─────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│     TransactionAggregation.Application      │  Business Logic Layer
│  (Services, Mappings, XML/JSON Serialization)
└─────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│   TransactionAggregation.Persistence        │  Data Access Layer
│  (Entity Framework Core, PostgreSQL)        │
└─────────────────────────────────────────────┘
                      
┌─────────────────────────────────────────────┐
│  TransactionAggregation.Processor           │  Message Processing
│  (Kafka Consumer, RabbitMQ Integration)     │
└─────────────────────────────────────────────┘
```

### Projects

- **TransactionAggregation.Api** - REST API with transaction endpoints, health checks, and Swagger documentation
- **TransactionAggregation.Application** - Business logic including:
  - Transaction service with filtering and aggregation
  - XML deserialization for transaction imports
  - AutoMapper profiles for DTOs
  - Custom exception handling
- **TransactionAggregation.Domain** - Core domain entities (Transaction, PaymentMethod, TransactionDirection)
- **TransactionAggregation.Contracts** - Data transfer objects and message contracts
- **TransactionAggregation.Persistence** - Entity Framework Core context, migrations, and repositories
- **TransactionAggregation.Processor** - Background service consuming messages from Kafka and RabbitMQ
- **TransactionAggregation.Tests** - Unit tests for transaction services

## Technologies

- **.NET 8** - Framework
- **PostgreSQL** - Relational database
- **Entity Framework Core** - ORM
- **Kafka** - Distributed message streaming
- **RabbitMQ** - Message broker
- **AutoMapper** - Object mapping
- **Docker & Docker Compose** - Containerization and orchestration
- **Swagger/OpenAPI** - API documentation

## Key Features

✅ **Multiple Data Sources** - Support for XML and JSON transaction imports  
✅ **Message Processing** - Asynchronous processing via Kafka/RabbitMQ  
✅ **Transaction Aggregation** - Summarize by customer, payment method, and direction  
✅ **Duplicate Detection** - Prevent duplicate transaction processing  
✅ **Advanced Filtering** - Query by customer, date range, payment method, and direction  
✅ **Exception Handling** - Centralized global exception handling with problem details  
✅ **API Documentation** - Swagger/OpenAPI integration  

## Getting Started

### Prerequisites

- .NET 8 SDK
- Docker & Docker Compose
- PostgreSQL 15+
- Kafka
- RabbitMQ

### Setup

#### Using Docker Compose (Recommended)

1. Clone the repository:
```bash
git clone <repository-url>
cd TransactionAPI
```

2. Create environment file (`.env`):
```env
POSTGRES_DB=transactions_db
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password
RABBITMQ_USER=guest
RABBITMQ_PASSWORD=guest
```

3. Start all services:
```bash
docker-compose up -d
```

This will start:
- PostgreSQL database (port 5432)
- RabbitMQ (port 5672, Management UI: 15672)
- Kafka (port 9092)
- Transaction API (port 8080)
- Transaction Processor (background service)

#### Local Development

1. Update connection strings in `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=transactions_db;Username=postgres;Password=your_password"
  }
}
```

2. Apply Entity Framework migrations:
```bash
dotnet ef database update -p TransactionAggregation.Persistence -s TransactionAggregation.Api
```

3. Run the API:
```bash
cd TransactionAggregation.Api
dotnet run
```

4. Run the Processor:
```bash
cd TransactionAggregation.Processor
dotnet run
```

## API Endpoints

### Transactions

**GET** `/api/transactions`  
Get all transactions

**GET** `/api/transactions/{id}`  
Get transaction by ID

**GET** `/api/transactions/customer/{customerId}`  
Get transactions by customer ID

**GET** `/api/transactions/customer/{customerId}/summary`  
Get customer transaction summary

**GET** `/api/transactions/customer/{customerId}/payment-methods`  
Get payment method summary for customer

**GET** `/api/transactions/customer/{customerId}/directions`  
Get transaction direction summary

**GET** `/api/transactions/customer/{customerId}/filter`  
Filter transactions by date range, payment method, and direction

### Health

**GET** `/health`  
Health check endpoint

## Transaction Domain Models

### Transaction
```csharp
public class Transaction
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Source { get; set; }
    public string ExternalTransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string Description { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public TransactionDirection Direction { get; set; }
}
```

### PaymentMethod
- Credit Card
- Debit Card
- Bank Transfer
- Digital Wallet
- Check

### TransactionDirection
- Inbound
- Outbound

## Message Processing

### Kafka Consumer
The processor service listens to the `transactions` topic on Kafka and processes incoming transaction messages. Failed messages are sent to the dead-letter topic (`transactions.dlq`) after max retries.

**Configuration:**
- Bootstrap Servers: `kafka:9092`
- Topic: `transactions`
- GroupId: `transaction-processor`
- Max Retries: 3
- Retry Delay: 2 seconds

### RabbitMQ Integration
RabbitMQ is configured for backup message handling with queue name `transactions`.

## Exception Handling

The API implements global exception handling that returns standardized `ProblemDetails` responses:

- **400 Bad Request** - Validation failures, duplicate transactions
- **500 Internal Server Error** - Unhandled exceptions
- **503 Service Unavailable** - Database/infrastructure errors

## Testing

Run unit tests:
```bash
dotnet test TransactionAggregation.Tests
```

## Database Migrations

Create a new migration:
```bash
dotnet ef migrations add <MigrationName> -p TransactionAggregation.Persistence -s TransactionAggregation.Api
```

Apply migrations:
```bash
dotnet ef database update -p TransactionAggregation.Persistence -s TransactionAggregation.Api
```

## Development Workflow

1. **Feature Branch**: Create a feature branch from `main`
2. **Code Changes**: Make your changes in the appropriate layer
3. **Tests**: Write/update unit tests
4. **Database**: Create migrations if needed
5. **Pull Request**: Submit for review
6. **Merge**: Merge to `main` after approval

## Project Structure

```
TransactionAPI/
├── TransactionAggregation.Api/          # REST API
│   ├── Controllers/                     # HTTP endpoints
│   ├── ExceptionHandling/               # Global exception handler
│   └── Program.cs                       # Startup configuration
├── TransactionAggregation.Application/  # Business logic
│   ├── Interfaces/                      # Service contracts
│   ├── Services/                        # Service implementations
│   ├── Serialization/                   # XML/JSON handling
│   ├── Mappings/                        # AutoMapper profiles
│   └── Exceptions/                      # Custom exceptions
├── TransactionAggregation.Domain/       # Domain entities
├── TransactionAggregation.Contracts/    # DTOs and messages
├── TransactionAggregation.Persistence/  # Data access
│   ├── Repositories/                    # Data repositories
│   ├── Migrations/                      # EF Core migrations
│   └── TransactionDbContext.cs          # DbContext
├── TransactionAggregation.Processor/    # Background processor
│   ├── Messaging/                       # Message handlers
│   ├── Kafka/                           # Kafka consumer
│   └── Program.cs                       # Worker startup
└── TransactionAggregation.Tests/        # Unit tests
```

## Configuration

Configuration is managed through:
- `appsettings.json` - Default settings
- `appsettings.Development.json` - Development overrides
- Environment variables - Runtime configuration
- Docker Compose `.env` file - Container configuration

## Deployment

### Docker

Build images:
```bash
docker build -f TransactionAggregation.Api/Dockerfile -t transaction-api:latest .
docker build -f TransactionAggregation.Processor/Dockerfile -t transaction-processor:latest .
```

Push to registry:
```bash
docker push transaction-api:latest
docker push transaction-processor:latest
```

### Kubernetes

Use the provided Docker images in your Kubernetes manifests. Ensure:
- PostgreSQL is running and accessible
- Kafka is configured with appropriate topics
- RabbitMQ is running and accessible

## Troubleshooting

### Database Connection Issues
- Verify PostgreSQL is running: `docker ps | grep postgres`
- Check connection string in configuration
- Ensure database migrations have been applied

### Kafka Consumer Not Processing Messages
- Verify Kafka is running: `docker ps | grep kafka`
- Check consumer group ID matches configuration
- Review processor logs for errors

### API Not Responding
- Check API container is running: `docker ps | grep transaction-api`
- Verify port 8080 is not in use locally
- Review API logs: `docker logs transaction-api`

## Contributing

1. Follow C# naming conventions and coding standards
2. Write unit tests for new features
3. Update database migrations for schema changes
4. Update this README for significant changes
