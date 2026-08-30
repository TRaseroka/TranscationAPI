using Microsoft.AspNetCore.Mvc;
using TransactionAggregation.Application.Interfaces;
using TransactionAggregation.Contracts;
using TransactionAggregation.Domain;
using TransactionAggregation.Contracts.Transactions;
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
public async Task<ActionResult<IReadOnlyList<TransactionResponseDto>>> GetAll(
    CancellationToken cancellationToken)
{
    var transactions = await _service.GetAllAsync(
        cancellationToken);

    return Ok(transactions);
}

[HttpGet("{id:guid}")]
public async Task<ActionResult<TransactionResponseDto>> GetById( Guid id, CancellationToken cancellationToken) 
{
     var transaction = await _service.GetByIdAsync( id, cancellationToken); 
   return transaction is null ?
    NotFound() : Ok(transaction); 
}

[HttpGet("customer/{customerId:guid}")]
public async Task<ActionResult<IReadOnlyList<TransactionResponseDto>>> GetByCustomer( Guid customerId, DateTime? from, DateTime? to, PaymentMethod? paymentMethod, TransactionDirection? direction, CancellationToken cancellationToken) 
{ 
    var transactions = await _service.GetByCustomerAsync( customerId, from, to, paymentMethod, direction, cancellationToken); 
     return Ok(transactions);
}

[HttpGet("customer/{customerId:guid}/summary")]
public async Task<ActionResult<CustomerTransactionSummary>> GetCustomerSummary(
    Guid customerId,
    CancellationToken cancellationToken)
{
    var summary = await _service.GetCustomerSummaryAsync(
        customerId,
        cancellationToken);

    return Ok(summary);
}

[HttpGet("customer/{customerId:guid}/by-payment-method")]
public async Task<ActionResult<IReadOnlyList<PaymentMethodSummary>>> GetByPaymentMethod(
    Guid customerId,
    CancellationToken cancellationToken)
{
    var summary = await _service.GetPaymentMethodSummaryAsync(
        customerId,
        cancellationToken);

    return Ok(summary);
}

[HttpGet("customer/{customerId:guid}/by-direction")]
public async Task<ActionResult<IReadOnlyList<TransactionDirectionSummary>>> GetByDirection(
    Guid customerId,
    CancellationToken cancellationToken)
{
    var summary = await _service.GetTransactionDirectionSummaryAsync(
        customerId,
        cancellationToken);

    return Ok(summary);
}

[HttpPost]
public async Task<IActionResult> Create(
    TransactionMessage message,
    CancellationToken cancellationToken)
{
    await _service.ProcessTransaction(
        message,
        cancellationToken);

    return CreatedAtAction(
        nameof(GetById),
        new { id = message.TransactionId },
        message);
}

}
