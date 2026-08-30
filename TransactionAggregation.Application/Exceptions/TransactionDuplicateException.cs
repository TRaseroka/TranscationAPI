namespace TransactionAggregation.Application.Exceptions;

public class TransactionDuplicateException : Exception
{
    public TransactionDuplicateType DuplicateType { get; }

    public TransactionDuplicateException(
        TransactionDuplicateType duplicateType,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        DuplicateType = duplicateType;
    }
}