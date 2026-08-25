using Amazon.SQS;
using Amazon.SQS.Model;
using FCG.Application.Interfaces;
using System.Text.Json;

namespace FCG.Infrastructure.Messaging
{
    public class SqsPublisher : ISqsPublisher
    {
        private readonly IAmazonSQS _sqsClient;

        public SqsPublisher(IAmazonSQS sqsClient)
        {
            _sqsClient = sqsClient;
        }

        public async Task PublicarAsync<T>(T evento, string fila) where T : class
        {
            var queueUrlResponse = await _sqsClient.GetQueueUrlAsync(fila);

            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrlResponse.QueueUrl,
                MessageBody = JsonSerializer.Serialize(evento)
            });
        }
    }
}
