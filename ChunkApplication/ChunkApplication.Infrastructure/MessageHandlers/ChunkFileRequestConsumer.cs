using System;
using System.Diagnostics;
using System.Text.Json;
using ChunkApplication.Application.Services;
using ChunkApplication.Infrastructure.MessageHandlers.Models;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading.Tasks;
using System.Linq;
using ChunkApplication.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ChunkApplication.Infrastructure.MessageHandlers;

public class ChunkFileRequestConsumer : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IChunkService _chunkService;
    private readonly ILogger<ChunkFileRequestConsumer> _logger;
    private readonly string _queueName = "ChunkFileRequest";
    private readonly string _responseQueueName = "FileProcessingResponse";

    public ChunkFileRequestConsumer(
        IConnection connection,
        IChunkService chunkService,
        ILogger<ChunkFileRequestConsumer> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _chunkService = chunkService ?? throw new ArgumentNullException(nameof(chunkService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("🔧 ChunkFileRequestConsumer constructor started");
        
        try
        {
            _channel = _connection.CreateModel();
            _logger.LogInformation("✅ RabbitMQ channel created successfully");
            
            SetupQueues();
            SetupConsumer();
            
            _logger.LogInformation("🎯 ChunkFileRequestConsumer is ready and listening on queue: {QueueName}", _queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ FAILED to initialize ChunkFileRequestConsumer");
            throw;
        }
    }

    private void SetupQueues()
    {
        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.QueueDeclare(
            queue: _responseQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _logger.LogInformation("Queues setup completed: {RequestQueue}, {ResponseQueue}", _queueName, _responseQueueName);
    }

    private void SetupConsumer()
    {
        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += (model, ea) =>
        {
            var requestId = string.Empty;
            
            _logger.LogInformation("🔔 MESSAGE RECEIVED! Processing chunk file request...");
            System.Console.WriteLine("🔔 MESSAGE RECEIVED! Processing chunk file request...");
            
            try
            {
                var body = ea.Body.ToArray();
                var messageJson = System.Text.Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<ChunkFileMessage>(messageJson);
                
                if (message == null)
                {
                    _logger.LogError("❌ Failed to deserialize message");
                    return;
                }

                requestId = message.RequestId;
                var filePaths = message.FilePath.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();
                
                _logger.LogInformation("🎯 Processing {FileCount} files from: {RequestId}", filePaths.Count, requestId);
                System.Console.WriteLine($"🎯 Processing {filePaths.Count} files from: {requestId}");

                foreach (var filePath in filePaths)
                {
                    try
                    {
                        System.Console.WriteLine($"📁 Chunking: {filePath}");
                        var fileEntity = _chunkService.ChunkFileAsync(filePath).Result;
                        System.Console.WriteLine($"✅ Chunked: {fileEntity.FileName} → {fileEntity.TotalChunks} chunks");
                    }
                    catch (Exception fileEx)
                    {
                        System.Console.WriteLine($"❌ Failed: {filePath} - {fileEx.Message}");
                    }
                }
                
                System.Console.WriteLine($"🎉 BATCH DONE: {requestId}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ ERROR: {requestId} - {ex.Message}");
            }
        };

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
        
        _channel.BasicConsume(
            queue: _queueName,
            autoAck: true,  
            consumer: consumer);

        _logger.LogInformation("Consumer setup completed for queue: {QueueName} with prefetch: 10", _queueName);
        System.Console.WriteLine($"🎯 Consumer ready for queue: {_queueName} (prefetch: 10)");
    }

    private async Task SendResponseAsync(FileProcessingResponse response)
    {
        try
        {
            var responseJson = JsonSerializer.Serialize(response);
            var responseBody = System.Text.Encoding.UTF8.GetBytes(responseJson);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: "",
                routingKey: _responseQueueName,
                basicProperties: properties,
                body: responseBody);

            _logger.LogDebug("Sent response for request: {RequestId}, Success: {Success}", 
                response.RequestId, response.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending response for request: {RequestId}", response.RequestId);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _logger.LogInformation("ChunkFileRequestConsumer disposed");
    }
}
