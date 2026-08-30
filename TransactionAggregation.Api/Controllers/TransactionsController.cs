using Microsoft.AspNetCore.Mvc;
using TransactionAggregation.Application.Interfaces;
using TransactionAggregation.Contracts;
using TransactionAggregation.Domain;
namespace TransactionAggregation.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
private readonly ITransactionService _service;


public TransactionsController(ITransactionService service)
{
    _service = service;
}

[HttpGet]
public async Task<IActionResult> GetAll(
    CancellationToken cancellationToken)
{
    var transactions = await _service.GetAllAsync(
        cancellationToken);

    return Ok(transactions);
}

[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(
    Guid id,
    CancellationToken cancellationToken)
{
    var transaction = await _service.GetByIdAsync(
        id,
        cancellationToken);

    return transaction is null
        ? NotFound()
        : Ok(transaction);
}

[HttpGet("customer/{customerId:guid}")]
public async Task<IActionResult> GetByCustomer(
    Guid customerId,
    DateTime? from,
    DateTime? to,
    PaymentMethod? paymentMethod,
    TransactionDirection? direction,
    CancellationToken cancellationToken)
{
    var transactions = await _service.GetByCustomerAsync(
        customerId,
        from,
        to,
        paymentMethod,
        direction,
        cancellationToken);

    return Ok(transactions);
}

[HttpGet("customer/{customerId:guid}/summary")]
public async Task<IActionResult> GetCustomerSummary(
    Guid customerId,
    CancellationToken cancellationToken)
{
    var summary = await _service.GetCustomerSummaryAsync(
        customerId,
        cancellationToken);

    return Ok(summary);
}

[HttpGet("customer/{customerId:guid}/by-payment-method")]
public async Task<IActionResult> GetByPaymentMethod(
    Guid customerId,
    CancellationToken cancellationToken)
{
    var summary = await _service.GetPaymentMethodSummaryAsync(
        customerId,
        cancellationToken);

    return Ok(summary);
}

[HttpGet("customer/{customerId:guid}/by-direction")]
public async Task<IActionResult> GetByDirection(
    Guid customerId,
    CancellationToken cancellationToken)
{
    var summary = await _service.GetTransactionDirectionSummaryAsync(
        customerId,
        cancellationToken);

    return Ok(summary);
}


}
