using System.Text;
using System.Text.Json;
using Atracciones.Shared.Messaging;
using Microsoft.Extensions.Options;
using MsFacturacion.Api.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MsFacturacion.Api.Services;

public class ReservasPagadasEventConsumer : BackgroundService
{
    private const string QueueName = "ms-facturacion.reservas-pagadas";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<ReservasPagadasEventConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public ReservasPagadasEventConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<ReservasPagadasEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("RabbitMQ esta deshabilitado para ms-facturacion.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                StartConsumer();
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo iniciar el consumidor RabbitMQ. Reintentando en 10 segundos.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private void StartConsumer()
    {
        var factory = CreateConnectionFactory();

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(QueueName, _options.ExchangeName, "reservas.reserva.pagada");
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, args) =>
        {
            try
            {
                await ProcessAsync(args.Body.ToArray(), args.BasicProperties.CorrelationId);
                _channel.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo procesar el evento reservas.reserva.pagada.");
                _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(QueueName, autoAck: false, consumer);
        _logger.LogInformation("Consumidor RabbitMQ activo en cola {QueueName}.", QueueName);
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        if (!string.IsNullOrWhiteSpace(_options.Uri))
        {
            return new ConnectionFactory
            {
                Uri = new Uri(_options.Uri),
                DispatchConsumersAsync = true
            };
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true
        };

        if (_options.UseSsl)
        {
            factory.Ssl.Enabled = true;
            factory.Ssl.ServerName = _options.HostName;
        }

        return factory;
    }

    private async Task ProcessAsync(byte[] body, string? correlationId)
    {
        var raw = Encoding.UTF8.GetString(body);
        var integrationEvent = JsonSerializer.Deserialize<IntegrationEvent>(raw, JsonOptions);
        if (integrationEvent?.Payload is not JsonElement payload)
            return;

        var metadata = TryGetObject(payload, "metadata");
        if (!TryGetGuid(payload, "rev_guid", out var reservaGuid))
            return;

        var request = new CrearFacturaRequest
        {
            ReservaGuid = reservaGuid,
            Numero = GetString(metadata, "fac_numero"),
            Total = GetDecimal(payload, "total", 0),
            Observacion = GetString(metadata, "observacion"),
            OrigenCanal = GetString(payload, "origen_canal") ?? "BOOKING",
            DatosFacturacion = new CrearDatosFacturacionRequest
            {
                Nombre = GetString(metadata, "nombre_receptor") ?? "Consumidor",
                Apellido = GetString(metadata, "apellido_receptor"),
                Correo = GetString(metadata, "correo_receptor") ?? "sin-correo@booking.local",
                Telefono = GetString(metadata, "telefono_receptor")
            }
        };

        using var scope = _scopeFactory.CreateScope();
        var facturaService = scope.ServiceProvider.GetRequiredService<FacturaService>();
        await facturaService.CrearAsync(request, CancellationToken.None);
        _logger.LogInformation("Evento reservas.reserva.pagada procesado para reserva {ReservaGuid}. CorrelationId: {CorrelationId}", reservaGuid, correlationId);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }

    private static JsonElement? TryGetObject(JsonElement source, string propertyName)
    {
        return source.ValueKind == JsonValueKind.Object &&
               source.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.Object
            ? value
            : null;
    }

    private static bool TryGetGuid(JsonElement source, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        return source.ValueKind == JsonValueKind.Object &&
               source.TryGetProperty(propertyName, out var element) &&
               element.ValueKind == JsonValueKind.String &&
               Guid.TryParse(element.GetString(), out value);
    }

    private static string? GetString(JsonElement? source, string propertyName)
    {
        if (source is not { ValueKind: JsonValueKind.Object } value ||
            !value.TryGetProperty(propertyName, out var property) ||
            property.ValueKind == JsonValueKind.Null)
            return null;

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static decimal GetDecimal(JsonElement source, string propertyName, decimal fallback)
    {
        if (source.ValueKind != JsonValueKind.Object ||
            !source.TryGetProperty(propertyName, out var property))
            return fallback;

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDecimal(out var value) => value,
            JsonValueKind.String when decimal.TryParse(property.GetString(), out var value) => value,
            _ => fallback
        };
    }
}
