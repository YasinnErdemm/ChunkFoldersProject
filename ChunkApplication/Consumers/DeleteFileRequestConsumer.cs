using ChunkApplication.Services;
using ChunkApplication.Interfaces;
using ChunkApplication.Messages.Requests;
using ChunkApplication.Messages.Responses;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ChunkApplication.Consumers;

/// <summary>
/// Consumer for processing delete file requests
/// </summary>
public class DeleteFileRequestConsumer : IConsumer<DeleteFileMessage>
{
    private readonly ILogger<DeleteFileRequestConsumer> _logger;
    private readonly IChunkService _chunkService;

    public DeleteFileRequestConsumer(
        ILogger<DeleteFileRequestConsumer> logger,
        IChunkService chunkService)
    {
        _logger = logger;
        _chunkService = chunkService;
    }

    public async Task Consume(ConsumeContext<DeleteFileMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing delete file request {RequestId} for file {FileId}", 
            message.RequestId, message.FileId);

        try
        {
            // Process the delete file request
            var success = await _chunkService.DeleteFileAsync(message.FileId);

            if (success)
            {
                _logger.LogInformation("Successfully processed delete file request {RequestId} for file {FileId}", 
                    message.RequestId, message.FileId);

                // Publish completion message
                await context.Publish(new FileProcessingMessage
                {
                    FileId = message.FileId,
                    Status = "Deleted",
                    Message = $"File deleted successfully",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                throw new Exception("File deletion failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process delete file request {RequestId}", message.RequestId);
            
            // Publish failure message
            await context.Publish(new FileProcessingMessage
            {
                FileId = message.FileId,
                Status = "Failed",
                Message = $"Failed to delete file {message.FileId}: {ex.Message}",
                CreatedAt = DateTime.UtcNow
            });
            
            throw;
        }
    }
}


