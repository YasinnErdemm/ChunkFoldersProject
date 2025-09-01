using System;
using System.Text;
using System.Text.Json;
using ChunkApplication.Application.Services;
using ChunkApplication.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChunkApplication.Infrastructure.MessageHandlers;

public class DeleteFileRequestConsumer : IDisposable
{
    private readonly IChunkService _chunkService;
    private readonly ILogger<DeleteFileRequestConsumer> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public DeleteFileRequestConsumer(
        IChunkService chunkService,
        ILogger<DeleteFileRequestConsumer> logger,
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
        _channel.QueueDeclare("DeleteFileRequest", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("delete-file-response", durable: true, exclusive: false, autoDelete: false);
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received delete file request: {Message}", message);
                var request = JsonSerializer.Deserialize<DeleteFileRequest>(message);
                if (request == null)
                {
                    _logger.LogError("Failed to deserialize delete file request");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                    return;
                }
                var success = await _chunkService.DeleteFileAsync(request.FileId);
                var response = new DeleteFileResponse
                {
                    RequestId = request.RequestId,
                    FileId = request.FileId,
                    Success = success,
                    Message = success ? "File deleted successfully" : "Failed to delete file",
                    Timestamp = DateTime.UtcNow
                };
                var responseJson = JsonSerializer.Serialize(response);
                var responseBody = Encoding.UTF8.GetBytes(responseJson);

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: "delete-file-response",
                    basicProperties: null,
                    body: responseBody);

                _logger.LogInformation("Delete file response sent successfully");

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing delete file request");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queue: "DeleteFileRequest", autoAck: false, consumer: consumer);
        _logger.LogInformation("DeleteFileRequestConsumer started successfully");
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _logger.LogInformation("DeleteFileRequestConsumer disposed");
    }
}

public class DeleteFileRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string FileId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class DeleteFileResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string FileId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
