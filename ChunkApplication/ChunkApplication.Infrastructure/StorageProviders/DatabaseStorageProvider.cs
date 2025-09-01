using System;
using System.IO;
using System.Threading.Tasks;
using ChunkApplication.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChunkApplication.Infrastructure.StorageProviders;

public class DatabaseStorageProvider : IStorageProvider
{
    public string Name => "Database";

    private readonly string _baseDirectory;
    private readonly ILogger<DatabaseStorageProvider> _logger;

    public DatabaseStorageProvider(ILogger<DatabaseStorageProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "chunk2");
        Directory.CreateDirectory(_baseDirectory);
        _logger.LogInformation("DatabaseStorageProvider initialized with base directory: {BaseDirectory}", _baseDirectory);
    }

    public async Task<string> StoreChunkAsync(string chunkId, byte[] data)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(chunkId))
                throw new ArgumentException("ChunkId cannot be null or empty", nameof(chunkId));

            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            var filePath = Path.Combine(_baseDirectory, $"{chunkId}.dbchunk");
            
            await File.WriteAllBytesAsync(filePath, data);
            
            _logger.LogDebug("Stored chunk {ChunkId} to database storage {FilePath}, size: {Size} bytes", 
                chunkId, filePath, data.Length);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing chunk {ChunkId} to database storage", chunkId);
            throw;
        }
    }

    public async Task<byte[]> RetrieveChunkAsync(string chunkId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(chunkId))
                throw new ArgumentException("ChunkId cannot be null or empty", nameof(chunkId));

            var filePath = Path.Combine(_baseDirectory, $"{chunkId}.dbchunk");
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Database chunk file not found: {filePath}");
            }

            var data = await File.ReadAllBytesAsync(filePath);
            
            _logger.LogDebug("Retrieved chunk {ChunkId} from database storage {FilePath}, size: {Size} bytes", 
                chunkId, filePath, data.Length);

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chunk {ChunkId} from database storage", chunkId);
            throw;
        }
    }

    public async Task DeleteChunkAsync(string chunkId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(chunkId))
                throw new ArgumentException("ChunkId cannot be null or empty", nameof(chunkId));

            var filePath = Path.Combine(_baseDirectory, $"{chunkId}.dbchunk");
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted chunk {ChunkId} from database storage {FilePath}", chunkId, filePath);
            }
            else
            {
                _logger.LogWarning("Database chunk file not found for deletion: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chunk {ChunkId} from database storage", chunkId);
            throw;
        }
    }

    public async Task<bool> ChunkExistsAsync(string chunkId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(chunkId))
                throw new ArgumentException("ChunkId cannot be null or empty", nameof(chunkId));

            var filePath = Path.Combine(_baseDirectory, $"{chunkId}.dbchunk");
            var exists = File.Exists(filePath);
            
            _logger.LogDebug("Database chunk {ChunkId} exists: {Exists}", chunkId, exists);
            
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database chunk existence: {ChunkId}", chunkId);
            return false;
        }
    }

    public async Task<long> GetStorageSizeAsync()
    {
        try
        {
            var directory = new DirectoryInfo(_baseDirectory);
            var totalSize = 0L;

            foreach (var file in directory.GetFiles("*.dbchunk", SearchOption.AllDirectories))
            {
                totalSize += file.Length;
            }

            _logger.LogDebug("Total database storage size: {TotalSize} bytes", totalSize);
            return totalSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating database storage size");
            return 0;
        }
    }

    public async Task<int> GetChunkCountAsync()
    {
        try
        {
            var directory = new DirectoryInfo(_baseDirectory);
            var chunkCount = directory.GetFiles("*.dbchunk", SearchOption.AllDirectories).Length;
            
            _logger.LogDebug("Total database chunk count: {ChunkCount}", chunkCount);
            return chunkCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating database chunk count");
            return 0;
        }
    }
}
