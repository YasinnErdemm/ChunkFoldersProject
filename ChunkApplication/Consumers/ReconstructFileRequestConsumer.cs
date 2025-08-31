using ChunkApplication.Services;
using ChunkApplication.Interfaces;
using ChunkApplication.Messages.Requests;
using ChunkApplication.Messages.Responses;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ChunkApplication.Consumers;

/// <summary>
/// Consumer for processing reconstruct file requests
/// </summary>
public class ReconstructFileRequestConsumer : IConsumer<ReconstructFileMessage>
{
    private readonly ILogger<ReconstructFileRequestConsumer> _logger;
    private readonly IChunkService _chunkService;

    public ReconstructFileRequestConsumer(
        ILogger<ReconstructFileRequestConsumer> logger,
        IChunkService chunkService)
    {
        _logger = logger;
        _chunkService = chunkService;
    }

    public async Task Consume(ConsumeContext<ReconstructFileMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing reconstruct file request {RequestId} for file {FileId}", 
            message.RequestId, message.FileId);

        try
        {
            // Define output directory (Docker container path)
            var outputDirectory = "/app/ChunkApplication_Output";

            // Ensure output directory exists
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Create output path
            var outputPath = Path.Combine(outputDirectory, message.OutputFileName);

            // Process the reconstruct file request
            var success = await _chunkService.ReconstructFileAsync(message.FileId, outputPath);

            if (success)
            {
                _logger.LogInformation("Successfully processed reconstruct file request {RequestId}. File reconstructed to: {OutputPath}", 
                    message.RequestId, outputPath);

                // Publish completion message
                await context.Publish(new FileProcessingMessage
                {
                    FileId = message.FileId,
                    Status = "Reconstructed",
                    Message = $"File reconstructed successfully to: {outputPath}",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                throw new Exception("File reconstruction failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process reconstruct file request {RequestId}", message.RequestId);
            
            // Publish failure message
            await context.Publish(new FileProcessingMessage
            {
                FileId = message.FileId,
                Status = "Failed",
                Message = $"Failed to reconstruct file {message.FileId}: {ex.Message}",
                CreatedAt = DateTime.UtcNow
            });
            
            throw;
        }
    }
}


