using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> GetAll(
    CancellationToken cancellationToken)
{
    var transactions = await _service.GetAllAsync(
        cancellationToken);

    return Ok(transactions);
}

[HttpGet("{id:guid}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> GetById(
    Guid id,
    CancellationToken cancellationToken)
{
    var transaction = await _service.GetByIdAsync(
        id,
        cancellationToken);

    if (transaction is null)
        return NotFound();

    return Ok(transaction);
}

[HttpGet("customer/{customerId:guid}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> GetByCustomer(
    Guid customerId,
    CancellationToken cancellationToken)
{
    var transactions = await _service.GetByCustomerIdAsync(
        customerId,
        cancellationToken);

    return Ok(transactions);
}

[HttpGet("customer/{customerId:guid}/summary")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> GetByDirection(
    Guid customerId,
    CancellationToken cancellationToken)
{
    var summary = await _service.GetTransactionDirectionSummaryAsync(
        customerId,
        cancellationToken);

    return Ok(summary);
}

[HttpPost]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
