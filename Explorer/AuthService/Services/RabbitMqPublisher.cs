using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace AuthService.Services;

public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly IChannel _channel;
    private readonly IConnection _connection;
    private const string ExchangeName = "user.registered";
    private readonly ILogger<RabbitMqPublisher> _logger;

    // Privatni konstruktor — koristimo factory metodu za async init
    private RabbitMqPublisher(IConnection connection, IChannel channel, ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection;
        _channel = channel;
        _logger = logger;
    }

    public static async Task<RabbitMqPublisher> CreateAsync(IConfiguration config, ILogger<RabbitMqPublisher> logger)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
            UserName = config["RabbitMQ:Username"] ?? "user",
            Password = config["RabbitMQ:Password"] ?? "pass"
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true);

        return new RabbitMqPublisher(connection, channel, logger);
    }

    public async Task PublishUserRegisteredAsync(int userId)
    {
        var message = new UserRegisteredEvent { UserId = userId };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var properties = new BasicProperties
        {
            Persistent = true
        };

        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Objavljen UserRegistered event za userId={UserId}", userId);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}