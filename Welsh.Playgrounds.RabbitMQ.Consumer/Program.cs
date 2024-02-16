using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var factory = new ConnectionFactory();
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();
channel.BasicQos(prefetchSize: 0, prefetchCount: 2, global: false);

var props = channel.CreateBasicProperties();
props.Persistent = true;

if (!int.TryParse(args[0], out var processTime))
    throw new ArgumentException("Process time value must be integer");

const string QUEUE_NAME = "Sample";

channel.QueueDeclare(
    queue: QUEUE_NAME,
    exclusive: false,
    durable: true,
    autoDelete: false,
    arguments: null
);

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (sender, args) =>
{
    var payload = args.Body.ToArray();
    var message = Encoding.UTF8.GetString(payload);
    Console.WriteLine($"{message}, {DateTime.Now}");
    Thread.Sleep(processTime);
    (sender as EventingBasicConsumer)!.Model.BasicAck(args.DeliveryTag, multiple: false);
};

channel.BasicConsume(
    queue: QUEUE_NAME,
    autoAck: false,
    consumer: consumer
);

Console.ReadLine();
