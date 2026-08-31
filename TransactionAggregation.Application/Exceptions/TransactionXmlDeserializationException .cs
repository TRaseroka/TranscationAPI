public class TransactionXmlDeserializationException : Exception
{
    public TransactionXmlDeserializationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}