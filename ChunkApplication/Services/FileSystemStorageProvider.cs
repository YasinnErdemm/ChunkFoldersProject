using ChunkApplication.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChunkApplication.Services;

/// <summary>
/// File system storage provider for storing chunks on local disk
/// </summary>
public class FileSystemStorageProvider : IStorageProvider
{
    private readonly ILogger<FileSystemStorageProvider> _logger;
    private readonly string _baseDirectory;

    public string ProviderName => "FileSystem";

    public FileSystemStorageProvider(ILogger<FileSystemStorageProvider> logger, string baseDirectory = null)
    {
        _logger = logger;
        
        // Use absolute path to ensure chunks are stored in the correct location
        if (string.IsNullOrEmpty(baseDirectory))
        {
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _baseDirectory = Path.Combine(appDirectory, "chunks");
        }
        else
        {
            _baseDirectory = Path.IsPathRooted(baseDirectory) 
                ? baseDirectory 
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, baseDirectory);
        }
        
        // Ensure base directory exists
        if (!Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
            _logger.LogInformation("Created base directory: {BaseDirectory}", _baseDirectory);
        }
    }

    public async Task StoreChunkAsync(string chunkId, byte[] data)
    {
        try
        {
            var filePath = Path.Combine(_baseDirectory, $"{chunkId}.chunk");
            await File.WriteAllBytesAsync(filePath, data);
            
            _logger.LogInformation("Stored chunk {ChunkId} to file system. Size: {Size} bytes", 
                chunkId, data.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store chunk {ChunkId} to file system", chunkId);
            throw;
        }
    }

    public async Task<byte[]> RetrieveChunkAsync(string chunkId)
    {
        try
        {
            var filePath = Path.Combine(_baseDirectory, $"{chunkId}.chunk");
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Chunk file not found: {filePath}");
            }

            var data = await File.ReadAllBytesAsync(filePath);
            _logger.LogInformation("Retrieved chunk {ChunkId} from file system. Size: {Size} bytes", 
                chunkId, data.Length);
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve chunk {ChunkId} from file system", chunkId);
            throw;
        }
    }

    public async Task DeleteChunkAsync(string chunkId)
    {
        try
        {
            var filePath = Path.Combine(_baseDirectory, $"{chunkId}.chunk");
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted chunk {ChunkId} from file system", chunkId);
            }
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete chunk {ChunkId} from file system", chunkId);
            throw;
        }
    }

    public async Task<bool> ChunkExistsAsync(string chunkId)
    {
        var filePath = Path.Combine(_baseDirectory, $"{chunkId}.chunk");
        return await Task.FromResult(File.Exists(filePath));
    }
}
