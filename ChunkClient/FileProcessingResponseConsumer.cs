using MassTransit;
using Microsoft.Extensions.Logging;

namespace ChunkClient;

/// <summary>
/// Consumer for processing file processing responses
/// </summary>
public class FileProcessingResponseConsumer : IConsumer<FileProcessingMessage>
{
    private readonly ILogger<FileProcessingResponseConsumer> _logger;

    public FileProcessingResponseConsumer(ILogger<FileProcessingResponseConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FileProcessingMessage> context)
    {
        var message = context.Message;
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

        await Task.CompletedTask;
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


