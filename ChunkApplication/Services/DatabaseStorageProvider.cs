using ChunkApplication.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChunkApplication.Services;

/// <summary>
/// Database storage provider for storing chunks in database
/// </summary>
public class DatabaseStorageProvider : IStorageProvider
{
    private readonly ILogger<DatabaseStorageProvider> _logger;
    private readonly Dictionary<string, byte[]> _inMemoryStorage;

    public string ProviderName => "Database";

    public DatabaseStorageProvider(ILogger<DatabaseStorageProvider> logger)
    {
        _logger = logger;
        _inMemoryStorage = new Dictionary<string, byte[]>();
    }

    public async Task StoreChunkAsync(string chunkId, byte[] data)
    {
        try
        {
            _inMemoryStorage[chunkId] = data;
            
            _logger.LogInformation("Stored chunk {ChunkId} to database storage. Size: {Size} bytes", 
                chunkId, data.Length);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store chunk {ChunkId} to database storage", chunkId);
            throw;
        }
    }

    public async Task<byte[]> RetrieveChunkAsync(string chunkId)
    {
        try
        {
            if (!_inMemoryStorage.TryGetValue(chunkId, out var data))
            {
                throw new KeyNotFoundException($"Chunk not found in database storage: {chunkId}");
            }

            _logger.LogInformation("Retrieved chunk {ChunkId} from database storage. Size: {Size} bytes", 
                chunkId, data.Length);
            
            return await Task.FromResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve chunk {ChunkId} from database storage", chunkId);
            throw;
        }
    }

    public async Task DeleteChunkAsync(string chunkId)
    {
        try
        {
            if (_inMemoryStorage.Remove(chunkId))
            {
                _logger.LogInformation("Deleted chunk {ChunkId} from database storage", chunkId);
            }
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete chunk {ChunkId} from database storage", chunkId);
            throw;
        }
    }

    public async Task<bool> ChunkExistsAsync(string chunkId)
    {
        return await Task.FromResult(_inMemoryStorage.ContainsKey(chunkId));
    }
}
