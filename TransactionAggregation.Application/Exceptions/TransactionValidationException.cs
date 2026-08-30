namespace TransactionAggregation.Application.Exceptions;

public class TransactionValidationException : Exception
{
    public TransactionValidationException(string message)
        : base(message)
    {
    }
}