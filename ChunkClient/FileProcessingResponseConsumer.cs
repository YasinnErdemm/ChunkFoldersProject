using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ChunkClient;

/// <summary>
/// Consumer for processing file processing responses
/// </summary>
public class FileProcessingResponseConsumer
{
    private readonly ILogger _logger;
    private readonly IModel _channel;

    public FileProcessingResponseConsumer(ILogger logger, IModel channel)
    {
        _logger = logger;
        _channel = channel;
    }

    public void StartConsuming()
    {
        // Declare queue
        _channel.QueueDeclare("FileProcessingResponse", durable: true, exclusive: false, autoDelete: false);

        // Create consumer
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<FileProcessingMessage>(messageJson);

                if (message != null)
                {
                    ProcessMessage(message);
                }

                // Acknowledge message
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file processing response");
                // Reject message
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        // Start consuming
        _channel.BasicConsume("FileProcessingResponse", false, consumer);
        _logger.LogInformation("Started consuming from FileProcessingResponse queue");
    }

    private void ProcessMessage(FileProcessingMessage message)
    {
        _logger.LogInformation("Received file processing response: {Status} - {Message}",
            message.Status, message.Message);

        // Handle different response types
        switch (message.Status)
        {
            case "Listed":
                // This is a list files response
                Console.WriteLine($"\n=== File List Response ===");
                Console.WriteLine($"Status: {message.Status}");
                Console.WriteLine($"Message: {message.Message}");
                Console.WriteLine("Note: Use option 6 to see the actual file list");
                break;

            case "Chunked":
                Console.WriteLine($"\n=== File Chunked Successfully ===");
                Console.WriteLine($"File ID: {message.FileId}");
                Console.WriteLine($"Status: {message.Status}");
                Console.WriteLine($"Message: {message.Message}");
                break;

            case "Reconstructed":
                Console.WriteLine($"\n=== File Reconstructed Successfully ===");
                Console.WriteLine($"File ID: {message.FileId}");
                Console.WriteLine($"Status: {message.Status}");
                Console.WriteLine($"Message: {message.Message}");
                break;

            case "Deleted":
                Console.WriteLine($"\n=== File Deleted Successfully ===");
                Console.WriteLine($"File ID: {message.FileId}");
                Console.WriteLine($"Status: {message.Status}");
                Console.WriteLine($"Message: {message.Message}");
                break;

            case "InfoRetrieved":
                Console.WriteLine($"\n=== File Info Retrieved ===");
                Console.WriteLine($"File ID: {message.FileId}");
                Console.WriteLine($"Status: {message.Status}");
                Console.WriteLine($"Message: {message.Message}");
                break;

            case "Failed":
                Console.WriteLine($"\n=== Operation Failed ===");
                Console.WriteLine($"File ID: {message.FileId}");
                Console.WriteLine($"Status: {message.Status}");
                Console.WriteLine($"Message: {message.Message}");
                break;

            default:
                Console.WriteLine($"\n=== Unknown Response ===");
                Console.WriteLine($"File ID: {message.FileId}");
                Console.WriteLine($"Status: {message.Status}");
                Console.WriteLine($"Message: {message.Message}");
                break;
        }
    }

    // File processing message class for ChunkClient
    public class FileProcessingMessage
    {
        public string FileId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

}


