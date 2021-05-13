using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Receiver
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() {HostName = "localhost"};

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(
                    queue: "rpc_queue",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                channel.BasicQos(0, 1, false);

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (sender, eventArgs) =>
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    var props = eventArgs.BasicProperties;

                    var ch = (sender as EventingBasicConsumer).Model;
                    
                    var replyProps = ch.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    var response = Fib(message);

                    var responseBytes = Encoding.UTF8.GetBytes(response);

                    ch.BasicPublish(
                        exchange: "",
                        routingKey: props.ReplyTo,
                        basicProperties: replyProps,
                        body: responseBytes);

                    ch.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
                };
                
                channel.BasicConsume(
                    queue: "rpc_queue",
                    autoAck: false,
                    consumer: consumer);
                
                Console.WriteLine("Waiting for tasks");
                

                Console.WriteLine("Press [enter] to exit");
                Console.ReadLine();
            }
        }

        private static string Fib(string message)
        {
            if (!int.TryParse(message, out var n))
                return string.Empty;

            var fib = new int[n];

            fib[0] = 0;
            fib[1] = 1;

            for (int i = 2; i < n; i++)
            {
                fib[i] = fib[i - 2] + fib[i - 1];
            }

            return fib[n - 1].ToString();
        }
    }
}