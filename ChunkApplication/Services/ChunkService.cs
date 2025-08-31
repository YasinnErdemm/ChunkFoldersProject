using System.Security.Cryptography;
using System.Text;
using ChunkApplication.Interfaces;
using ChunkApplication.Models;
using ChunkApplication.Repositories;
using Microsoft.Extensions.Logging;
using MassTransit;

namespace ChunkApplication.Services;

/// <summary>
/// Main service for handling file chunking and reconstruction operations
/// </summary>
public class ChunkService : IChunkService
{
    private readonly ILogger<ChunkService> _logger;
    private readonly IFileRepository _fileRepository;
    private readonly IEnumerable<IStorageProvider> _storageProviders;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly Random _random;

    public ChunkService(
        ILogger<ChunkService> logger,
        IFileRepository fileRepository,
        IEnumerable<IStorageProvider> storageProviders,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _storageProviders = storageProviders;
        _publishEndpoint = publishEndpoint;
        _random = new Random();
    }

    public async Task<FileMetadata> ChunkFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            _logger.LogInformation("Starting to chunk file: {FilePath}", filePath);

            var fileInfo = new FileInfo(filePath);
            var fileId = GenerateFileId();
            var checksum = await CalculateFileChecksumAsync(filePath);
            
            // Dinamik chunk'lama algoritması: Dosya boyutuna göre otomatik karar
            var (actualChunkSize, totalChunks) = CalculateDynamicChunking(fileInfo.Length);
            
            var fileMetadata = new FileMetadata
            {
                Id = fileId,
                FileName = fileInfo.Name,
                OriginalPath = filePath,
                FileSize = fileInfo.Length,
                Checksum = checksum,
                CreatedAt = DateTime.UtcNow,
                ChunkSize = actualChunkSize,
                TotalChunks = totalChunks
            };

            var chunks = new List<ChunkInfo>();
            var storageProvidersArray = _storageProviders.ToArray();

            using var fileStream = File.OpenRead(filePath);
            var chunkNumber = 0;
            var remainingBytes = fileInfo.Length;

            // HEDEF CHUNK SAYISINA GÖRE BÖL
            while (chunkNumber < totalChunks && remainingBytes > 0)
            {
                // Son chunk için kalan byte'ları hesapla
                var currentChunkSize = (int)Math.Min(actualChunkSize, remainingBytes);
                
                // Eğer son chunk ise ve hala chunk sayısı hedefe ulaşmadıysa, kalan tüm byte'ları al
                if (chunkNumber == totalChunks - 1)
                {
                    currentChunkSize = (int)remainingBytes;
                }
                
                var chunkId = $"{fileId}_chunk_{chunkNumber}";
                var chunkData = new byte[currentChunkSize];
                
                // Dosyadan chunk'ı oku
                var bytesRead = await fileStream.ReadAsync(chunkData, 0, currentChunkSize);
                if (bytesRead == 0) break;

                // Select a random storage provider for this chunk
                var selectedProvider = storageProvidersArray[_random.Next(storageProvidersArray.Length)];

                // Create chunk info
                var chunkInfo = new ChunkInfo
                {
                    Id = chunkId,
                    FileId = fileId,
                    ChunkNumber = chunkNumber,
                    ChunkSize = bytesRead,
                    StorageProvider = selectedProvider.ProviderName,
                    Checksum = CalculateChunkChecksum(chunkData),
                    CreatedAt = DateTime.UtcNow
                };

                chunks.Add(chunkInfo);

                // Publish chunk message to RabbitMQ for async processing
                var chunkMessage = new ChunkMessage
                {
                    FileId = fileId,
                    ChunkId = chunkId,
                    ChunkData = chunkData,
                    ChunkNumber = chunkNumber,
                    StorageProvider = selectedProvider.ProviderName,
                    Checksum = chunkInfo.Checksum,
                    CreatedAt = DateTime.UtcNow
                };

                await _publishEndpoint.Publish(chunkMessage);
                _logger.LogInformation("Published chunk message for {ChunkId} to queue", chunkId);
                chunkNumber++;
                remainingBytes -= bytesRead;
            }

            fileMetadata.Chunks = chunks;

            // Save file metadata to database
            await _fileRepository.AddAsync(fileMetadata);
            await _fileRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully chunked file {FileName} into {ChunkCount} chunks", 
                fileMetadata.FileName, fileMetadata.TotalChunks);

            return fileMetadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to chunk file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<bool> ReconstructFileAsync(string fileId, string outputPath)
    {
        try
        {
            _logger.LogInformation("Starting to reconstruct file: {FileId} to {OutputPath}", fileId, outputPath);

            var fileMetadata = await _fileRepository.GetByIdAsync(fileId);
            if (fileMetadata == null)
            {
                _logger.LogWarning("File metadata not found for ID: {FileId}", fileId);
                return false;
            }

            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Get all storage providers
            var storageProviders = _storageProviders.ToDictionary(sp => sp.ProviderName, sp => sp);

            using var outputStream = File.Create(outputPath);
            var reconstructedChecksum = string.Empty;

            // Reconstruct file from chunks
            foreach (var chunk in fileMetadata.Chunks.OrderBy(c => c.ChunkNumber))
            {
                if (!storageProviders.TryGetValue(chunk.StorageProvider, out var provider))
                {
                    _logger.LogError("Storage provider not found: {ProviderName}", chunk.StorageProvider);
                    return false;
                }

                var chunkData = await provider.RetrieveChunkAsync(chunk.Id);
                
                // Verify chunk checksum
                var calculatedChecksum = CalculateChunkChecksum(chunkData);
                if (calculatedChecksum != chunk.Checksum)
                {
                    _logger.LogError("Chunk checksum mismatch for chunk {ChunkId}", chunk.Id);
                    return false;
                }

                await outputStream.WriteAsync(chunkData, 0, chunkData.Length);
            }

            outputStream.Close();

            // Verify final file checksum
            reconstructedChecksum = await CalculateFileChecksumAsync(outputPath);
            if (reconstructedChecksum != fileMetadata.Checksum)
            {
                _logger.LogError("Reconstructed file checksum mismatch for file {FileId}", fileId);
                File.Delete(outputPath); // Clean up corrupted file
                return false;
            }

            // Update last accessed time
            fileMetadata.LastAccessed = DateTime.UtcNow;
            await _fileRepository.UpdateAsync(fileMetadata);
            await _fileRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully reconstructed file {FileName} to {OutputPath}", 
                fileMetadata.FileName, outputPath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconstruct file: {FileId}", fileId);
            return false;
        }
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(string fileId)
    {
        try
        {
            return await _fileRepository.GetByIdAsync(fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file metadata for ID: {FileId}", fileId);
            return null;
        }
    }

    public async Task<IEnumerable<FileMetadata>> ListFilesAsync()
    {
        try
        {
            return await _fileRepository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files");
            return Enumerable.Empty<FileMetadata>();
        }
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        try
        {
            var fileMetadata = await _fileRepository.GetByIdAsync(fileId);
            if (fileMetadata == null)
            {
                return false;
            }

            // Delete all chunks from storage providers
            var storageProviders = _storageProviders.ToDictionary(sp => sp.ProviderName, sp => sp);

            foreach (var chunk in fileMetadata.Chunks)
            {
                if (storageProviders.TryGetValue(chunk.StorageProvider, out var provider))
                {
                    await provider.DeleteChunkAsync(chunk.Id);
                }
            }

            // Delete file metadata from database
            await _fileRepository.DeleteAsync(fileId);
            await _fileRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted file {FileName} and all its chunks", fileMetadata.FileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FileId}", fileId);
            return false;
        }
    }

    private string GenerateFileId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private async Task<string> CalculateFileChecksumAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(fileStream);
        return Convert.ToHexString(hash);
    }

    private string CalculateChunkChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Dosya boyutuna göre dinamik chunk'lama kararı verir
    /// </summary>
    private (int chunkSize, int totalChunks) CalculateDynamicChunking(long fileSize)
    {
        const long oneMB = 1024 * 1024;
        const long fiveMB = 5 * oneMB;
        const long tenMB = 10 * oneMB;
        const long fiftyMB = 50 * oneMB;
        
        int targetChunks;
        
        // Dosya boyutuna göre chunk sayısını belirle
        if (fileSize < oneMB)
        {
            targetChunks = 2; // 1 MB'den küçük: 2 chunk
        }
        else if (fileSize < fiveMB)
        {
            targetChunks = 3; // 1-5 MB arası: 3 chunk
        }
        else if (fileSize < tenMB)
        {
            targetChunks = 4; // 5-10 MB arası: 4 chunk
        }
        else if (fileSize < fiftyMB)
        {
            targetChunks = 5; // 10-50 MB arası: 5 chunk
        }
        else
        {
            // 50+ MB: Her 10 MB'de +1 chunk
            targetChunks = 6 + (int)((fileSize - fiftyMB) / (10 * oneMB));
            targetChunks = Math.Min(targetChunks, 20); // Maximum 20 chunk
        }
        
        // Chunk boyutunu hesapla - HEDEF CHUNK SAYISINA GÖRE
        var chunkSize = (int)(fileSize / targetChunks);
        
        // Eğer chunk boyutu çok küçükse, chunk sayısını azalt
        const int minChunkSize = 1024; // 1 KB
        if (chunkSize < minChunkSize && targetChunks > 2)
        {
            // Chunk sayısını azalt, ama minimum 2'de tut
            targetChunks = Math.Max(2, (int)(fileSize / minChunkSize));
            chunkSize = (int)(fileSize / targetChunks);
        }
        
        _logger.LogInformation("File size: {FileSize} bytes, Target chunks: {TargetChunks}, Chunk size: {ChunkSize} bytes", 
            fileSize, targetChunks, chunkSize);
        
        return (chunkSize, targetChunks);
    }
}
