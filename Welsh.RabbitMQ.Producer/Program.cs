using RabbitMQ.Client;
using System.Text;

var factory = new ConnectionFactory { HostName = "localhost" };

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();
const string QUEUE_NAME = "Sample";

channel.QueueDeclare(
    queue: QUEUE_NAME,
    exclusive: false,
    durable: true,
    autoDelete: false,
    arguments: null
);

var properties = channel.CreateBasicProperties();
properties.Persistent = true;

using var cts = new CancellationTokenSource();

var props = channel.CreateBasicProperties();
props.Persistent = true;
while (!cts.IsCancellationRequested)
{
    var message = Console.ReadLine();
    var messageBody = Encoding.UTF8.GetBytes(message);

    channel.BasicPublish(
        exchange: "",
        routingKey: QUEUE_NAME,
        basicProperties: props,
        body: messageBody
    );
}


