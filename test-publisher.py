import pika
import json
import os

credentials = pika.PlainCredentials(
    os.environ["RABBITMQ_USER"],
    os.environ["RABBITMQ_PASSWORD"]
)

connection = pika.BlockingConnection(
    pika.ConnectionParameters(
        host="rabbitmq",
        port=5672,
        credentials=credentials
    )
)

channel = connection.channel()

message = {
    "transactionId": "22222222-2222-2222-2222-222222222222",
    "customerId": "11111111-1111-1111-1111-111111111111",
    "source": "TestBank",
    "externalTransactionId": "EXT-DOCKER-001",
    "transactionDate": "2026-08-26T07:30:00Z",
    "amount": 750.00,
    "currency": "ZAR",
    "description": "Docker processor test",
    "paymentMethod": "EFT",
    "direction": "Credit"
}

channel.basic_publish(
    exchange="",
    routing_key="transactions",
    body=json.dumps(message)
)

print("Message published successfully")

connection.close()
