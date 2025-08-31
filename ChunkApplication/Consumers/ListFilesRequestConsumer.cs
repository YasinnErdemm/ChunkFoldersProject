using ChunkApplication.Services;
using ChunkApplication.Interfaces;
using ChunkApplication.Messages.Requests;
using ChunkApplication.Messages.Responses;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ChunkApplication.Consumers;

/// <summary>
/// Consumer for processing list files requests
/// </summary>
public class ListFilesRequestConsumer : IConsumer<ListFilesMessage>
{
    private readonly ILogger<ListFilesRequestConsumer> _logger;
    private readonly IChunkService _chunkService;

    public ListFilesRequestConsumer(
        ILogger<ListFilesRequestConsumer> logger,
        IChunkService chunkService)
    {
        _logger = logger;
        _chunkService = chunkService;
    }

    public async Task Consume(ConsumeContext<ListFilesMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("=== LISTFILES CONSUMER ÇALIŞIYOR! ===");
        _logger.LogInformation("Processing list files request {RequestId}", message.RequestId);

        try
        {
            // Process the list files request
            var files = await _chunkService.ListFilesAsync();

            _logger.LogInformation("Successfully processed list files request {RequestId}. Found {FileCount} files", 
                message.RequestId, files.Count());

            // Publish completion message with file details
            await context.Publish(new FileProcessingMessage
            {
               
                FileId = message.RequestId,
                Status = "Listed",
                Message = $"Found {files.Count()} files in the system. File IDs: {string.Join(", ", files.Select(f => f.Id))}",
                CreatedAt = DateTime.UtcNow
            });

            // Also publish individual file messages for each file
            foreach (var file in files)
            {
                await context.Publish(new FileListMessage
                {
                    FileId = file.Id,
                    FileName = file.FileName,
                    FileSize = file.FileSize,
                    TotalChunks = file.TotalChunks,
                    CreatedAt = file.CreatedAt,
                    RequestId = message.RequestId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process list files request {RequestId}", message.RequestId);
            
            // Publish failure message
            await context.Publish(new FileProcessingMessage
            {
                FileId = message.RequestId,
                Status = "Failed",
                Message = $"Failed to list files: {ex.Message}",
                CreatedAt = DateTime.UtcNow
            });
            
            throw;
        }
    }
}





