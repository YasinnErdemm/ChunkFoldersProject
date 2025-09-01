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
        
        // Clear any old messages from the queue (optional - uncomment if needed)
        // _channel.QueuePurge("ReconstructFileRequest");
        // _logger.LogInformation("Purged old messages from ReconstructFileRequest queue");

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
                System.Console.WriteLine($"🔍 RAW MESSAGE: {message}");

                // Parse request - try flexible parsing
                string requestId = "";
                string fileId = "";
                string outputPath = "";
                
                try
                {
                    // First try standard format
                    var request = JsonSerializer.Deserialize<ReconstructFileRequest>(message);
                    if (request != null)
                    {
                        requestId = request.RequestId;
                        fileId = request.FileId;
                        outputPath = request.OutputPath ?? "";
                    }
                    
                    // If OutputPath is still empty, try parsing as dynamic JSON
                    if (string.IsNullOrEmpty(outputPath))
                    {
                        using var doc = JsonDocument.Parse(message);
                        var root = doc.RootElement;
                        
                        requestId = root.TryGetProperty("RequestId", out var reqId) ? reqId.GetString() ?? "" : "";
                        fileId = root.TryGetProperty("FileId", out var fId) ? fId.GetString() ?? "" : "";
                        
                        // Try both OutputPath and OutputFileName
                        if (root.TryGetProperty("OutputPath", out var outPath))
                        {
                            outputPath = outPath.GetString() ?? "";
                        }
                        else if (root.TryGetProperty("OutputFileName", out var outFileName))
                        {
                            outputPath = outFileName.GetString() ?? "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse reconstruct file request");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                    return;
                }

                if (string.IsNullOrEmpty(fileId))
                {
                    _logger.LogError("Invalid request - missing FileId");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                    return;
                }

                System.Console.WriteLine($"🔍 PARSED REQUEST:");
                System.Console.WriteLine($"   - RequestId: {requestId}");
                System.Console.WriteLine($"   - FileId: {fileId}");
                System.Console.WriteLine($"   - OutputPath: '{outputPath}'");

                // Default output directory
                var outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "output");
                
                // Ensure directory exists
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                    _logger.LogInformation("Created output directory: {OutputDirectory}", outputDirectory);
                }
                
                // Handle the OutputPath - ALWAYS use the filename provided by user
                var userInput = outputPath?.Trim() ?? "";
                string fileName;
                
                if (!string.IsNullOrEmpty(userInput))
                {
                    // User provided a filename - ALWAYS use it (even if it's "output")
                    fileName = Path.GetFileName(userInput);
                    
                    // If user just wrote "output", treat it as "output.txt"
                    if (string.IsNullOrEmpty(fileName) || fileName.Equals("output", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName = "output.txt";
                    }
                    
                    _logger.LogInformation("Using user-provided filename: {FileName}", fileName);
                    System.Console.WriteLine($"📝 Using user-provided filename: {fileName}");
                }
                else
                {
                    // Only if user provided nothing at all, use original filename
                    var fileEntity = await _chunkService.GetFileInfoAsync(fileId);
                    fileName = fileEntity?.FileName ?? $"reconstructed_{fileId}_{DateTime.Now:yyyyMMdd_HHmmss}";
                    _logger.LogInformation("No filename provided, using original filename: {FileName}", fileName);
                    System.Console.WriteLine($"📝 No filename provided, using original filename: {fileName}");
                }
                
                var fullOutputPath = Path.Combine(outputDirectory, fileName);
                
                _logger.LogInformation("Reconstructing file to: {OutputPath}", fullOutputPath);
                System.Console.WriteLine($"📁 Reconstructing to: {fullOutputPath}");
                
                // Process the request
                var success = await _chunkService.ReconstructFileAsync(fileId, fullOutputPath);

                // Create response
                var response = new ReconstructFileResponse
                {
                    RequestId = requestId,
                    FileId = fileId,
                    OutputPath = fullOutputPath,
                    Success = success,
                    Message = success ? $"File reconstructed successfully to output/{fileName}" : "Failed to reconstruct file",
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
