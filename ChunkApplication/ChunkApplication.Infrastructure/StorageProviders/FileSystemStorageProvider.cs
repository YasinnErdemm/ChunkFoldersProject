using System;
using System.IO;
using System.Threading.Tasks;
using ChunkApplication.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChunkApplication.Infrastructure.StorageProviders;
public class FileSystemStorageProvider : IStorageProvider
{
    public string Name => "FileSystem";

    private readonly string _baseDirectory;
    private readonly ILogger<FileSystemStorageProvider> _logger;

    public FileSystemStorageProvider(ILogger<FileSystemStorageProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Create base directory for chunks
        _baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "chunks");
        Directory.CreateDirectory(_baseDirectory);
        
        _logger.LogInformation("FileSystemStorageProvider initialized with base directory: {BaseDirectory}", _baseDirectory);
    }

    public async Task<string> StoreChunkAsync(string chunkId, byte[] data)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(chunkId))
                throw new ArgumentException("ChunkId cannot be null or empty", nameof(chunkId));

            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            var filePath = Path.Combine(_baseDirectory, $"{chunkId}.chunk");
            
            await File.WriteAllBytesAsync(filePath, data);
            
            _logger.LogDebug("Stored chunk {ChunkId} to {FilePath}, size: {Size} bytes", 
                chunkId, filePath, data.Length);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing chunk {ChunkId}", chunkId);
            throw;
        }
    }

    public async Task<byte[]> RetrieveChunkAsync(string chunkId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(chunkId))
                throw new ArgumentException("ChunkId cannot be null or empty", nameof(chunkId));

            var filePath = Path.Combine(_baseDirectory, $"{chunkId}.chunk");
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Chunk file not found: {filePath}");
            }

            var data = await File.ReadAllBytesAsync(filePath);
            
            _logger.LogDebug("Retrieved chunk {ChunkId} from {FilePath}, size: {Size} bytes", 
                chunkId, filePath, data.Length);

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chunk {ChunkId}", chunkId);
            throw;
        }
    }

    public async Task DeleteChunkAsync(string chunkId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(chunkId))
                throw new ArgumentException("ChunkId cannot be null or empty", nameof(chunkId));

            var filePath = Path.Combine(_baseDirectory, $"{chunkId}.chunk");
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted chunk {ChunkId} from {FilePath}", chunkId, filePath);
            }
            else
            {
                _logger.LogWarning("Chunk file not found for deletion: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chunk {ChunkId}", chunkId);
            throw;
        }
    }

    public async Task<bool> ChunkExistsAsync(string chunkId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(chunkId))
                throw new ArgumentException("ChunkId cannot be null or empty", nameof(chunkId));

            var filePath = Path.Combine(_baseDirectory, $"{chunkId}.chunk");
            var exists = File.Exists(filePath);
            
            _logger.LogDebug("Chunk {ChunkId} exists: {Exists}", chunkId, exists);
            
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking chunk existence: {ChunkId}", chunkId);
            return false;
        }
    }

    public async Task<long> GetStorageSizeAsync()
    {
        try
        {
            var directory = new DirectoryInfo(_baseDirectory);
            var totalSize = 0L;

            foreach (var file in directory.GetFiles("*.chunk", SearchOption.AllDirectories))
            {
                totalSize += file.Length;
            }

            _logger.LogDebug("Total storage size: {TotalSize} bytes", totalSize);
            return totalSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating storage size");
            return 0;
        }
    }

    public async Task<int> GetChunkCountAsync()
    {
        try
        {
            var directory = new DirectoryInfo(_baseDirectory);
            var chunkCount = directory.GetFiles("*.chunk", SearchOption.AllDirectories).Length;
            
            _logger.LogDebug("Total chunk count: {ChunkCount}", chunkCount);
            return chunkCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating chunk count");
            return 0;
        }
    }
}
