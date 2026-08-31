using System.Xml;
using System.Xml.Serialization;
using TransactionAggregation.Application.Exceptions;
using TransactionAggregation.Contracts;

namespace TransactionAggregation.Application.Serialization;

public class TransactionXmlDeserializer
{
    private readonly XmlSerializer _serializer;

    public TransactionXmlDeserializer()
    {
        _serializer = new XmlSerializer(typeof(TransactionXmlMessage));
    }

    public TransactionMessage Deserialize(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new TransactionXmlDeserializationException(
                "Kafka message is empty.",
                new InvalidOperationException(
                    "The Kafka message contains no XML."));
        }

        try
        {
            using var reader = new StringReader(xml);

            var xmlMessage =
                _serializer.Deserialize(reader) as TransactionXmlMessage;

            if (xmlMessage is null)
            {
                throw new TransactionXmlDeserializationException(
                    "XML message could not be deserialized.",
                    new InvalidOperationException(
                        "XmlSerializer returned a null result."));
            }

            return MapToTransactionMessage(xmlMessage);
        }
        catch (TransactionXmlDeserializationException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            throw new TransactionXmlDeserializationException(
                "Kafka message contains invalid XML or does not match " +
                "the expected transaction XML structure.",
                ex);
        }
        catch (XmlException ex)
        {
            throw new TransactionXmlDeserializationException(
                "Kafka message contains malformed XML.",
                ex);
        }
    }

    private static TransactionMessage MapToTransactionMessage(
        TransactionXmlMessage xmlMessage)
    {
        return new TransactionMessage
        {
            TransactionId = xmlMessage.TransactionId,
            CustomerId = xmlMessage.CustomerId,
            Source = xmlMessage.Source,
            ExternalTransactionId = xmlMessage.ExternalTransactionId,
            TransactionDate = xmlMessage.TransactionDate,
            Amount = xmlMessage.Amount,
            Currency = xmlMessage.Currency,
            Description = xmlMessage.Description,
            PaymentMethod = xmlMessage.PaymentMethod,
            Direction = xmlMessage.Direction
        };
    }
}