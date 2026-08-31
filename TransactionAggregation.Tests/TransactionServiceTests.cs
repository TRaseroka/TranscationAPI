using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using TransactionAggregation.Application.Exceptions;
using TransactionAggregation.Application.Mappings;
using TransactionAggregation.Application.Services;
using TransactionAggregation.Contracts;
using TransactionAggregation.Domain;
using TransactionAggregation.Persistence.Repositories;

namespace TransactionAggregation.Tests;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _repositoryMock;
    private readonly IMapper _mapper;
    private readonly TransactionService _service;

public TransactionServiceTests()
{
    _repositoryMock = new Mock<ITransactionRepository>();

    using var loggerFactory =
        LoggerFactory.Create(builder => { });

    var mapperConfig = new MapperConfiguration(
        cfg =>
        {
            cfg.AddProfile<TransactionMappingProfile>();
        },
        loggerFactory);

    _mapper = mapperConfig.CreateMapper();

    _service = new TransactionService(
        _repositoryMock.Object,
        _mapper);
}

    [Fact]
    public async Task ProcessTransaction_WhenTransactionIdIsDuplicate_ThrowsTransactionDuplicateException()
    {
        // Arrange
        var transactionId =
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var message = CreateValidMessage(transactionId);

        var postgresException = CreatePostgresException(
            "PK_Transactions");

        var dbUpdateException =
            new Microsoft.EntityFrameworkCore.DbUpdateException(
                "Duplicate transaction ID.",
                postgresException);

        _repositoryMock
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbUpdateException);

        // Act
        var exception = await Assert.ThrowsAsync<TransactionDuplicateException>(
            () => _service.ProcessTransaction(message));

        // Assert
        Assert.Equal(
            TransactionDuplicateType.TransactionId,
            exception.DuplicateType);

        Assert.Contains(
            transactionId.ToString(),
            exception.Message);
    }

    [Fact]
    public async Task ProcessTransaction_WhenSourceAndExternalIdAreDuplicate_ThrowsTransactionDuplicateException()
    {
        // Arrange
        var transactionId =
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var message = CreateValidMessage(transactionId);

        var postgresException = CreatePostgresException(
            "UX_Transactions_Source_ExternalTransactionId");

        var dbUpdateException =
            new Microsoft.EntityFrameworkCore.DbUpdateException(
                "Duplicate source and external transaction ID.",
                postgresException);

        _repositoryMock
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbUpdateException);

        // Act
        var exception = await Assert.ThrowsAsync<TransactionDuplicateException>(
            () => _service.ProcessTransaction(message));

        // Assert
        Assert.Equal(
            TransactionDuplicateType.SourceAndExternalTransactionId,
            exception.DuplicateType);

        Assert.Contains(
            message.Source,
            exception.Message);

        Assert.Contains(
            message.ExternalTransactionId,
            exception.Message);
    }

    [Fact]
    public async Task ProcessTransaction_WhenTransactionIsValid_SavesTransaction()
    {
        // Arrange
        var message = CreateValidMessage(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

        // Act
        await _service.ProcessTransaction(message);

        // Assert
        _repositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Transaction>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Fact]
public async Task ProcessTransaction_WhenTransactionIdIsEmpty_ThrowsValidationException()
{
    // Arrange
    var message = CreateValidMessage(Guid.Empty);

    // Act
    var exception = await Assert.ThrowsAsync<TransactionValidationException>(
        () => _service.ProcessTransaction(message));

    // Assert
    Assert.Equal("Transaction ID is required.", exception.Message);
}

[Fact]
public async Task ProcessTransaction_WhenCustomerIdIsEmpty_ThrowsValidationException()
{
    // Arrange
    var message = CreateValidMessage(
        Guid.Parse("11111111-1111-1111-1111-111111111111"));

    message.CustomerId = Guid.Empty;

    // Act
    var exception = await Assert.ThrowsAsync<TransactionValidationException>(
        () => _service.ProcessTransaction(message));

    // Assert
    Assert.Equal("Customer ID is required.", exception.Message);
}

[Fact]
public async Task ProcessTransaction_WhenSourceIsEmpty_ThrowsValidationException()
{
    // Arrange
    var message = CreateValidMessage(
        Guid.Parse("11111111-1111-1111-1111-111111111111"));

    message.Source = string.Empty;

    // Act
    var exception = await Assert.ThrowsAsync<TransactionValidationException>(
        () => _service.ProcessTransaction(message));

    // Assert
    Assert.Equal("Transaction source is required.", exception.Message);
}

[Fact]
public async Task ProcessTransaction_WhenExternalTransactionIdIsEmpty_ThrowsValidationException()
{
    // Arrange
    var message = CreateValidMessage(
        Guid.Parse("11111111-1111-1111-1111-111111111111"));

    message.ExternalTransactionId = string.Empty;

    // Act
    var exception = await Assert.ThrowsAsync<TransactionValidationException>(
        () => _service.ProcessTransaction(message));

    // Assert
    Assert.Equal(
        "External transaction ID is required.",
        exception.Message);
}

[Fact]
public async Task ProcessTransaction_WhenTransactionDateIsDefault_ThrowsValidationException()
{
    // Arrange
    var message = CreateValidMessage(
        Guid.Parse("11111111-1111-1111-1111-111111111111"));

    message.TransactionDate = default;

    // Act
    var exception = await Assert.ThrowsAsync<TransactionValidationException>(
        () => _service.ProcessTransaction(message));

    // Assert
    Assert.Equal(
        "Transaction date is required.",
        exception.Message);
}

[Fact]
public async Task ProcessTransaction_WhenAmountIsZero_ThrowsValidationException()
{
    // Arrange
    var message = CreateValidMessage(
        Guid.Parse("11111111-1111-1111-1111-111111111111"));

    message.Amount = 0;

    // Act
    var exception = await Assert.ThrowsAsync<TransactionValidationException>(
        () => _service.ProcessTransaction(message));

    // Assert
    Assert.Equal(
        "Transaction amount must be greater than zero.",
        exception.Message);
}

[Fact]
public async Task ProcessTransaction_WhenAmountIsNegative_ThrowsValidationException()
{
    // Arrange
    var message = CreateValidMessage(
        Guid.Parse("11111111-1111-1111-1111-111111111111"));

    message.Amount = -100;

    // Act
    var exception = await Assert.ThrowsAsync<TransactionValidationException>(
        () => _service.ProcessTransaction(message));

    // Assert
    Assert.Equal(
        "Transaction amount must be greater than zero.",
        exception.Message);
}

[Fact]
public async Task ProcessTransaction_WhenCurrencyIsEmpty_ThrowsValidationException()
{
    // Arrange
    var message = CreateValidMessage(
        Guid.Parse("11111111-1111-1111-1111-111111111111"));

    message.Currency = string.Empty;

    // Act
    var exception = await Assert.ThrowsAsync<TransactionValidationException>(
        () => _service.ProcessTransaction(message));

    // Assert
    Assert.Equal(
        "Currency is required.",
        exception.Message);
}

[Fact]
public async Task ProcessTransaction_WhenPaymentMethodIsInvalid_ThrowsValidationException()
{
    // Arrange
    var message = CreateValidMessage(
        Guid.Parse("11111111-1111-1111-1111-111111111111"));

    message.PaymentMethod = (PaymentMethod)999;

    // Act
    var exception = await Assert.ThrowsAsync<TransactionValidationException>(
        () => _service.ProcessTransaction(message));

    // Assert
    Assert.Equal(
        "Invalid payment method.",
        exception.Message);
}

[Fact]
public async Task ProcessTransaction_WhenDirectionIsInvalid_ThrowsValidationException()
{
    // Arrange
    var message = CreateValidMessage(
        Guid.Parse("11111111-1111-1111-1111-111111111111"));

    message.Direction = (TransactionDirection)999;

    // Act
    var exception = await Assert.ThrowsAsync<TransactionValidationException>(
        () => _service.ProcessTransaction(message));

    // Assert
    Assert.Equal(
        "Invalid transaction direction.",
        exception.Message);
}

    private static TransactionMessage CreateValidMessage(Guid transactionId)
    {
        return new TransactionMessage
        {
            TransactionId = transactionId,
            CustomerId =
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Source = "BankTest",
            ExternalTransactionId =
                $"TEST-{Guid.NewGuid():N}",
            TransactionDate = DateTime.UtcNow,
            Amount = 1000m,
            Currency = "ZAR",
            Description = "Automated test transaction",
            PaymentMethod = PaymentMethod.PayShap,
            Direction = TransactionDirection.Credit
        };
    }

    private static PostgresException CreatePostgresException(
        string constraintName)
    {
        return new PostgresException(
            "duplicate key value violates unique constraint",
            "ERROR",
            "ERROR",
            "23505",
            detail: null,
            hint: null,
            position: 0,
            internalPosition: 0,
            internalQuery: null,
            where: null,
            schemaName: "public",
            tableName: "Transactions",
            columnName: null,
            dataTypeName: null,
            constraintName: constraintName,
            file: "nbtinsert.c",
            line: "666",
            routine: "_bt_check_unique");
    }
}