using System;
using System.IO;
using System.Text;
using System.Text.Json;
using ChunkApplication.Application.Services;
using ChunkApplication.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChunkApplication.Infrastructure.MessageHandlers;

public class ReconstructFileRequestConsumer : IDisposable
{
    private readonly IChunkService _chunkService;
    private readonly ILogger<ReconstructFileRequestConsumer> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public ReconstructFileRequestConsumer(
        IChunkService chunkService,
        ILogger<ReconstructFileRequestConsumer> logger,
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
        // Request queue
        _channel.QueueDeclare("ReconstructFileRequest", durable: true, exclusive: false, autoDelete: false);

        // Response queue
        _channel.QueueDeclare("reconstruct-file-response", durable: true, exclusive: false, autoDelete: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Received reconstruct file request: {Message}", message);

                // Parse request
                var request = JsonSerializer.Deserialize<ReconstructFileRequest>(message);
                if (request == null)
                {
                    _logger.LogError("Failed to deserialize reconstruct file request");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                    return;
                }

                // Default output directory
                var outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "output");
                
                // Ensure directory exists
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                    _logger.LogInformation("Created output directory: {OutputDirectory}", outputDirectory);
                }
                
                // request.OutputPath is the filename user provided
                var fileName = request.OutputPath;
                var fullOutputPath = Path.Combine(outputDirectory, fileName);
                
                _logger.LogInformation("Reconstructing file to: {OutputPath}", fullOutputPath);
                System.Console.WriteLine($"📁 Reconstructing to: {fullOutputPath}");
                
                // Process the request
                var success = await _chunkService.ReconstructFileAsync(request.FileId, fullOutputPath);

                // Create response
                var response = new ReconstructFileResponse
                {
                    RequestId = request.RequestId,
                    FileId = request.FileId,
                    OutputPath = fullOutputPath,  // Full path including Output2
                    Success = success,
                    Message = success ? $"File reconstructed successfully to Output2/{request.OutputPath}" : "Failed to reconstruct file",
                    Timestamp = DateTime.UtcNow
                };

                // Send response
                var responseJson = JsonSerializer.Serialize(response);
                var responseBody = Encoding.UTF8.GetBytes(responseJson);

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: "reconstruct-file-response",
                    basicProperties: null,
                    body: responseBody);

                _logger.LogInformation("Reconstruct file response sent successfully");

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reconstruct file request");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queue: "ReconstructFileRequest", autoAck: false, consumer: consumer);
        _logger.LogInformation("ReconstructFileRequestConsumer started successfully");
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _logger.LogInformation("ReconstructFileRequestConsumer disposed");
    }
}

public class ReconstructFileRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string FileId { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ReconstructFileResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string FileId { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
