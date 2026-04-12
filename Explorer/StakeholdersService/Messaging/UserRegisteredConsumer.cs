using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StakeholdersService.Services;
using System.Text;
using System.Text.Json;

namespace StakeholdersService.Messaging;

public class UserRegisteredEvent
{
    public int UserId { get; set; }
}

public class UserRegisteredConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<UserRegisteredConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "user.registered";
    private const string QueueName = "stakeholders.user.registered";

    public UserRegisteredConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<UserRegisteredConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Retry dok RabbitMQ ne bude spreman (važno u Dockeru)
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndConsumeAsync(stoppingToken);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RabbitMQ nije spreman, pokušavam ponovo za 5s: {Message}", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }

        // Čekamo dok servis ne bude zaustavljen
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
    }

    private async Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_config["RabbitMQ:Port"] ?? "5672"),
            UserName = _config["RabbitMQ:Username"] ?? "user",
            Password = _config["RabbitMQ:Password"] ?? "pass"
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: string.Empty,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Stakeholders consumer povezan na RabbitMQ, sluša queue: {Queue}", QueueName);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var body = Encoding.UTF8.GetString(args.Body.ToArray());

        try
        {
            var evt = JsonSerializer.Deserialize<UserRegisteredEvent>(body);
            if (evt is null)
            {
                _logger.LogWarning("Primljena null poruka, odbacujem");
                await _channel!.BasicNackAsync(args.DeliveryTag, false, false);
                return;
            }

            _logger.LogInformation("Primljen UserRegistered event za userId={UserId}", evt.UserId);

            using var scope = _scopeFactory.CreateScope();
            var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
            await profileService.InitializeAsync(evt.UserId);

            await _channel!.BasicAckAsync(args.DeliveryTag, false);
            _logger.LogInformation("Profil kreiran za userId={UserId}", evt.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška pri obradi UserRegistered eventa, vraćam u queue");
            await _channel!.BasicNackAsync(args.DeliveryTag, false, requeue: true);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
        await base.StopAsync(cancellationToken);
    }
}