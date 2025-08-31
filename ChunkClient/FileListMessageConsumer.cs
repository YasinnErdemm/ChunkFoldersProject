using MassTransit;
using Microsoft.Extensions.Logging;

namespace ChunkClient;

/// <summary>
/// Consumer for processing file list messages
/// </summary>
public class FileListMessageConsumer : IConsumer<FileListMessage>
{
    private readonly ILogger<FileListMessageConsumer> _logger;

    public FileListMessageConsumer(ILogger<FileListMessageConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FileListMessage> context)
    {
        var message = context.Message;
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

        await Task.CompletedTask;
    }
}

// File list message class
public class FileListMessage
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int TotalChunks { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RequestId { get; set; } = string.Empty;
}
