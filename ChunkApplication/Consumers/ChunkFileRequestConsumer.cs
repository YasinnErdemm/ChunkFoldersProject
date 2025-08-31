using ChunkApplication.Services;
using ChunkApplication.Interfaces;
using ChunkApplication.Messages.Requests;
using ChunkApplication.Messages.Responses;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ChunkApplication.Consumers;

/// <summary>
/// Consumer for processing chunk file requests
/// </summary>
public class ChunkFileRequestConsumer : IConsumer<ChunkFileMessage>
{
    private readonly ILogger<ChunkFileRequestConsumer> _logger;
    private readonly IChunkService _chunkService;

    public ChunkFileRequestConsumer(
        ILogger<ChunkFileRequestConsumer> logger,
        IChunkService chunkService)
    {
        _logger = logger;
        _chunkService = chunkService;
    }

    public async Task Consume(ConsumeContext<ChunkFileMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("=== CONSUMER ÇALIŞIYOR! ===");
        _logger.LogInformation("Processing chunk file request {RequestId} for file {FilePath}", 
            message.RequestId, message.FilePath);

        try
        {
            // Process the chunk file request
            var fileMetadata = await _chunkService.ChunkFileAsync(message.FilePath);

            _logger.LogInformation("Successfully processed chunk file request {RequestId}. File ID: {FileId}", 
                message.RequestId, fileMetadata.Id);

            // Publish completion message
            await context.Publish(new FileProcessingMessage
            {
                FileId = fileMetadata.Id,
                Status = "Chunked",
                Message = $"File {fileMetadata.FileName} chunked successfully into {fileMetadata.TotalChunks} chunks",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process chunk file request {RequestId}", message.RequestId);
            
            // Publish failure message
            await context.Publish(new FileProcessingMessage
            {
                FileId = message.RequestId,
                Status = "Failed",
                Message = $"Failed to chunk file {message.FilePath}: {ex.Message}",
                CreatedAt = DateTime.UtcNow
            });
            
            throw;
        }
    }
}


