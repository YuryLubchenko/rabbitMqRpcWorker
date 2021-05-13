using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Sender
{
    public class RpcClient: IDisposable
    {
        private const string QueueName = "rpc_queue";
        
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _replyQueueName;
        private readonly EventingBasicConsumer _consumer;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<int>> _callbackMapper = new();

        public RpcClient()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _channel.QueueDeclare(
                queue: QueueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            
            _replyQueueName = _channel.QueueDeclare().QueueName;
            _consumer = new EventingBasicConsumer(_channel);
            _consumer.Received += (sender, args) =>
            {
                if (!_callbackMapper.TryRemove(args.BasicProperties.CorrelationId, out var tcs))
                    return;

                var responseMessage = Encoding.UTF8.GetString(args.Body.ToArray());

                var result = int.TryParse(responseMessage, out var n) ? n : 0;

                tcs.TrySetResult(result);
            };
        }

        public Task<int> CallAsync(int n, CancellationToken cancellationToken = default)
        {
            var props = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = _replyQueueName;

            var message = n.ToString();
            var messageBytes = Encoding.UTF8.GetBytes(message);

            var tcs = new TaskCompletionSource<int>();
            _callbackMapper.TryAdd(correlationId, tcs);
            
            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: QueueName,
                basicProperties: props,
                body: messageBytes);

            _channel.BasicConsume(
                consumer: _consumer,
                queue: _replyQueueName,
                autoAck: true);

            cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out _));

            return tcs.Task;
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}