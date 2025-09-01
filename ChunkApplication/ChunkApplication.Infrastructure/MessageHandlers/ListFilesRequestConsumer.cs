using System;
using System.Text;
using System.Text.Json;
using ChunkApplication.Application.Services;
using ChunkApplication.Infrastructure.MessageHandlers.Models;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Linq;
using ChunkApplication.Domain.Interfaces;
using ChunkApplication.ChunkApplication.Infrastructure.MessageHandlers.Models;

namespace ChunkApplication.Infrastructure.MessageHandlers;

public class ListFilesRequestConsumer : IDisposable
{
    private readonly IChunkService _chunkService;
    private readonly ILogger<ListFilesRequestConsumer> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public ListFilesRequestConsumer(
        IChunkService chunkService,
        ILogger<ListFilesRequestConsumer> logger,
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
        _channel.QueueDeclare("ListFilesRequest", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("FileListResponse", durable: true, exclusive: false, autoDelete: false);
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Received list files request: {Message}", message);
                var files = await _chunkService.ListFilesAsync();
                var response = new FileListResponse
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Files = files.Select(f => new ChunkApplication.Infrastructure.MessageHandlers.Models.FileInfo
                    {
                        Id = f.Id,
                        FileName = f.FileName,
                        FileSize = f.FileSize,
                        TotalChunks = f.TotalChunks,
                        CreatedAt = f.CreatedAt,
                        IsComplete = f.IsComplete()
                    }).ToList(),
                    Success = true,
                    Message = $"Found {files.Count()} file(s)",
                    Timestamp = DateTime.UtcNow
                };

                var responseJson = JsonSerializer.Serialize(response);
                var responseBody = Encoding.UTF8.GetBytes(responseJson);

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: "FileListResponse",
                    basicProperties: null,
                    body: responseBody);

                _logger.LogInformation("List files response sent successfully");

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing list files request");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queue: "ListFilesRequest", autoAck: false, consumer: consumer);
        _logger.LogInformation("ListFilesRequestConsumer started successfully");
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _logger.LogInformation("ListFilesRequestConsumer disposed");
    }
}

