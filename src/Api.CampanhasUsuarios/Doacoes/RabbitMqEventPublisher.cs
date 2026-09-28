using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Api.CampanhasUsuarios.Doacoes;

public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly RabbitMqOptions _options;

    public RabbitMqEventPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public Task PublicarAsync(DoacaoRecebidaEvent evento)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: _options.Fila, durable: true, exclusive: false, autoDelete: false);

        var corpo = JsonSerializer.SerializeToUtf8Bytes(evento);
        var propriedades = channel.CreateBasicProperties();
        propriedades.Persistent = true;

        channel.BasicPublish(exchange: string.Empty, routingKey: _options.Fila, basicProperties: propriedades, body: corpo);

        return Task.CompletedTask;
    }
}
