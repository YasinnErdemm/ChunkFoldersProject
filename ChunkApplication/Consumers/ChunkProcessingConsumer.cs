using ChunkApplication.Models;
using ChunkApplication.Services;
using ChunkApplication.Data;
using ChunkApplication.Interfaces;
using ChunkApplication.Messages.Requests;
using ChunkApplication.Messages.Responses;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ChunkApplication.Consumers;

/// <summary>
/// MassTransit consumer for processing chunk messages
/// </summary>
public class ChunkProcessingConsumer : IConsumer<ChunkMessage>
{
    private readonly ILogger<ChunkProcessingConsumer> _logger;
    private readonly IStorageProvider _fileSystemStorage;
    private readonly IStorageProvider _databaseStorage;
    private readonly ChunkDbContext _dbContext;

    public ChunkProcessingConsumer(
        ILogger<ChunkProcessingConsumer> logger,
        IStorageProvider fileSystemStorage,
        IStorageProvider databaseStorage,
        ChunkDbContext dbContext)
    {
        _logger = logger;
        _fileSystemStorage = fileSystemStorage;
        _databaseStorage = databaseStorage;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<ChunkMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing chunk {ChunkId} for file {FileId}", message.ChunkId, message.FileId);

        try
        {
            // Store chunk based on storage provider
            IStorageProvider targetProvider = message.StorageProvider switch
            {
                "FileSystem" => _fileSystemStorage,
                "Database" => _databaseStorage,
                _ => throw new ArgumentException($"Unknown storage provider: {message.StorageProvider}")
            };

            await targetProvider.StoreChunkAsync(message.ChunkId, message.ChunkData);

            // Update chunk info in database
            var chunkInfo = new ChunkInfo
            {
                Id = message.ChunkId,
                FileId = message.FileId,
                ChunkNumber = message.ChunkNumber,
                ChunkSize = message.ChunkData.Length,
                StorageProvider = message.StorageProvider,
                Checksum = message.Checksum
            };

            _dbContext.Chunks.Add(chunkInfo);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully processed chunk {ChunkId} using {StorageProvider}", 
                message.ChunkId, message.StorageProvider);

            // Publish completion message
            await context.Publish(new FileProcessingMessage
            {
                FileId = message.FileId,
                Status = "ChunkProcessed",
                Message = $"Chunk {message.ChunkNumber} processed successfully",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process chunk {ChunkId}", message.ChunkId);
            
            // Publish failure message
            await context.Publish(new FileProcessingMessage
            {
                FileId = message.FileId,
                Status = "Failed",
                Message = $"Failed to process chunk {message.ChunkNumber}: {ex.Message}",
                CreatedAt = DateTime.UtcNow
            });
            
            throw;
        }
    }
}
