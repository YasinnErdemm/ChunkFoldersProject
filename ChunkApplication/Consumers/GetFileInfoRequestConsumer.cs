using ChunkApplication.Services;
using ChunkApplication.Interfaces;
using ChunkApplication.Messages.Requests;
using ChunkApplication.Messages.Responses;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ChunkApplication.Consumers;

/// <summary>
/// Consumer for processing get file info requests
/// </summary>
public class GetFileInfoRequestConsumer : IConsumer<GetFileInfoMessage>
{
    private readonly ILogger<GetFileInfoRequestConsumer> _logger;
    private readonly IChunkService _chunkService;

    public GetFileInfoRequestConsumer(
        ILogger<GetFileInfoRequestConsumer> logger,
        IChunkService chunkService)
    {
        _logger = logger;
        _chunkService = chunkService;
    }

    public async Task Consume(ConsumeContext<GetFileInfoMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing get file info request {RequestId} for file {FileId}", 
            message.RequestId, message.FileId);

        try
        {
            // Process the get file info request
            var fileMetadata = await _chunkService.GetFileMetadataAsync(message.FileId);

            if (fileMetadata != null)
            {
                _logger.LogInformation("Successfully processed get file info request {RequestId} for file {FileId}", 
                    message.RequestId, message.FileId);

                // Publish completion message
                await context.Publish(new FileProcessingMessage
                {
                    FileId = message.FileId,
                    Status = "InfoRetrieved",
                    Message = $"File info retrieved successfully for {fileMetadata.FileName}",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                throw new Exception("File not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process get file info request {RequestId}", message.RequestId);
            
            // Publish failure message
            await context.Publish(new FileProcessingMessage
            {
                FileId = message.FileId,
                Status = "Failed",
                Message = $"Failed to get file info for {message.FileId}: {ex.Message}",
                CreatedAt = DateTime.UtcNow
            });
            
            throw;
        }
    }
}


