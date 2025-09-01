using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ChunkClient;

/// <summary>
/// Consumer for processing file list messages
/// </summary>
public class FileListMessageConsumer
{
    private readonly ILogger _logger;
    private readonly IModel _channel;

    public FileListMessageConsumer(ILogger logger, IModel channel)
    {
        _logger = logger;
        _channel = channel;
    }

    public void StartConsuming()
    {
        // Declare queue
        _channel.QueueDeclare("FileListResponse", durable: true, exclusive: false, autoDelete: false);

        // Create consumer
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                var response = JsonSerializer.Deserialize<FileListResponse>(messageJson);

                if (response != null && response.Success && response.Files != null)
                {
                    ProcessFileListResponse(response);
                }

                // Acknowledge message
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file list message");
                // Reject message
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        // Start consuming
        _channel.BasicConsume("FileListResponse", false, consumer);
        _logger.LogInformation("Started consuming from FileListResponse queue");
    }

    private void ProcessFileListResponse(FileListResponse response)
    {
        _logger.LogInformation("Received file list response with {FileCount} files", response.Files.Count);
        
        // Clear existing cache and add all files from response
        var newFileList = new List<FileInfo>();
        
        foreach (var file in response.Files)
        {
            var fileInfo = new FileInfo
            {
                Id = file.Id,
                FileName = file.FileName,
                FileSize = file.FileSize,
                TotalChunks = file.TotalChunks,
                CreatedAt = file.CreatedAt
            };
            
            newFileList.Add(fileInfo);
            _logger.LogInformation("Added file to cache: {FileName} (ID: {FileId})", fileInfo.FileName, fileInfo.Id);
        }
        
        // Update the cached file list
        Program.UpdateFileList(newFileList);
        
        Console.WriteLine($"\n✅ File list updated! Found {newFileList.Count} files.");
    }

    private void ProcessMessage(FileListMessage message)
    {
        _logger.LogInformation("Received file list message: {FileName} (ID: {FileId})", 
            message.FileName, message.FileId);

        // Create FileInfo object and add to cache
        var fileInfo = new FileInfo
        {
            Id = message.FileId,
            FileName = message.FileName,
            FileSize = message.FileSize,
            TotalChunks = message.TotalChunks,
            CreatedAt = message.CreatedAt
        };

        // Add to the cached file list (don't replace, append)
        var currentList = Program.GetCurrentFileList();
        
        // Check if file already exists to avoid duplicates
        var existingFile = currentList.FirstOrDefault(f => f.Id == fileInfo.Id);
        if (existingFile == null)
        {
            currentList.Add(fileInfo);
            Program.UpdateFileList(currentList);
            _logger.LogInformation("Added file to cache: {FileName} (ID: {FileId})", fileInfo.FileName, fileInfo.Id);
        }
        else
        {
            _logger.LogInformation("File already in cache: {FileName} (ID: {FileId})", fileInfo.FileName, fileInfo.Id);
        }
    }
}

// File list response class
public class FileListResponse
{
    public string RequestId { get; set; } = string.Empty;
    public List<FileInfoResponse> Files { get; set; } = new();
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class FileInfoResponse
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int TotalChunks { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsComplete { get; set; }
}

// File list message class (backward compatibility)
public class FileListMessage
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int TotalChunks { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RequestId { get; set; } = string.Empty;
}
