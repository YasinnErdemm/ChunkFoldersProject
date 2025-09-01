using System;
using System.Text;
using System.Text.Json;
using ChunkApplication.Application.Services;
using ChunkApplication.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChunkApplication.Infrastructure.MessageHandlers;

public class GetFileInfoRequestConsumer : IDisposable
{
    private readonly IChunkService _chunkService;
    private readonly ILogger<GetFileInfoRequestConsumer> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public GetFileInfoRequestConsumer(
        IChunkService chunkService,
        ILogger<GetFileInfoRequestConsumer> logger,
        IConnection connection)
    {
        _chunkService = chunkService;
        _logger = logger;
        _connection = connection;
        _channel = _connection.CreateModel();

        SetupQueues();
    }

    private void SetupQueues()
    {
        _channel.QueueDeclare("GetFileInfoRequest", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("get-file-info-response", durable: true, exclusive: false, autoDelete: false);
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received get file info request: {Message}", message);
                var request = JsonSerializer.Deserialize<GetFileInfoRequest>(message);
                if (request == null)
                {
                    _logger.LogError("Failed to deserialize get file info request");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                    return;
                }
                var file = await _chunkService.GetFileInfoAsync(request.FileId);
                var response = new GetFileInfoResponse
                {
                    RequestId = request.RequestId,
                    FileId = request.FileId,
                    Success = file != null,
                    Message = file != null ? "File found" : "File not found",
                    Timestamp = DateTime.UtcNow
                };

                if (file != null)
                {
                    response.FileName = file.FileName;
                    response.FileSize = file.FileSize;
                    response.TotalChunks = file.TotalChunks;
                    response.CreatedAt = file.CreatedAt;
                    response.IsComplete = file.IsComplete();
                    response.Integrity = file.ValidateIntegrity();
                }
                var responseJson = JsonSerializer.Serialize(response);
                var responseBody = Encoding.UTF8.GetBytes(responseJson);

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: "get-file-info-response",
                    basicProperties: null,
                    body: responseBody);

                _logger.LogInformation("Get file info response sent successfully");

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing get file info request");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queue: "GetFileInfoRequest", autoAck: false, consumer: consumer);
        _logger.LogInformation("GetFileInfoRequestConsumer started successfully");
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _logger.LogInformation("GetFileInfoRequestConsumer disposed");
    }
}

public class GetFileInfoRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string FileId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class GetFileInfoResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int TotalChunks { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsComplete { get; set; }
    public bool Integrity { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
