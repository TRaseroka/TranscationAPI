using TransactionAggregation.Application.Interfaces;
using TransactionAggregation.Contracts;

namespace TransactionAggregation.Processor.Messaging;

public class TransactionMessageHandler : ITransactionMessageHandler
{
    private readonly ITransactionService _transactionService;

    public TransactionMessageHandler(
        ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    public async Task HandleAsync(
        TransactionMessage message,
        CancellationToken cancellationToken)
    {
        await _transactionService.ProcessTransaction(
            message,
            cancellationToken);
    }
}