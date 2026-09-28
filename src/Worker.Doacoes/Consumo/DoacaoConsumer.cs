using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Worker.Doacoes.Doacoes;

namespace Worker.Doacoes.Consumo;

public class DoacaoConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<DoacaoConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public DoacaoConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitMqOptions> options, ILogger<DoacaoConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(queue: _options.Fila, durable: true, exclusive: false, autoDelete: false);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (sender, eventArgs) =>
        {
            try
            {
                var corpo = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var evento = JsonSerializer.Deserialize<DoacaoRecebidaEvent>(corpo);

                if (evento is not null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<DoacaoProcessor>();
                    await processor.ProcessarAsync(evento, stoppingToken);
                }

                _channel!.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                if (eventArgs.Redelivered)
                {
                    _logger.LogError(ex, "Falha ao processar doacao pela segunda vez, descartando mensagem para evitar loop infinito. DeliveryTag: {DeliveryTag}", eventArgs.DeliveryTag);
                    try
                    {
                        _channel!.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    }
                    catch (Exception nackEx)
                    {
                        _logger.LogError(nackEx, "Falha ao enviar Nack apos segunda tentativa (canal provavelmente fechado durante shutdown)");
                    }
                }
                else
                {
                    _logger.LogError(ex, "Falha ao processar doacao, mensagem sera reenfileirada para uma unica nova tentativa. DeliveryTag: {DeliveryTag}", eventArgs.DeliveryTag);
                    try
                    {
                        _channel!.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
                    }
                    catch (Exception nackEx)
                    {
                        _logger.LogError(nackEx, "Falha ao enviar Nack para reenfileiramento (canal provavelmente fechado durante shutdown)");
                    }
                }
            }
        };

        _channel!.BasicConsume(queue: _options.Fila, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();
        await base.StopAsync(cancellationToken);
    }
}
