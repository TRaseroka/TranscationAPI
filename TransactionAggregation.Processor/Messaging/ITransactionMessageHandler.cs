using TransactionAggregation.Contracts;

namespace TransactionAggregation.Processor.Messaging;

public interface ITransactionMessageHandler
{
    Task HandleAsync(
        TransactionMessage message,
        CancellationToken cancellationToken);
}