using FCG.Application.Interfaces;
using System.Text.Json;
using RabbitMQ.Client;
using System.Text;

namespace FCG.Infrastructure.Messaging
{
    public class RabbitMQPublisher : IMessagePublisher
    {
        private readonly IConnection _connection;

        public RabbitMQPublisher(IConnection connection)
        {
            _connection = connection;
        }

        public async Task PublicarAsync<T>(T evento, string fila) where T : class
        {
            using var channel = await _connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync("dlx", ExchangeType.Direct, durable: true);

            await channel.QueueDeclareAsync(
                queue: $"{fila}.dlq",
                durable: true,
                exclusive: false,
                autoDelete: false
            );
            await channel.QueueBindAsync($"{fila}.dlq", "dlx", fila);

            var args = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", "dlx" },
                { "x-dead-letter-routing-key", fila }
            };

            await channel.QueueDeclareAsync(
                queue: fila,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args
            );

            var json = JsonSerializer.Serialize(evento);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: fila,
                mandatory: false,
                basicProperties: props,
                body: body
            );
        }
    }
}