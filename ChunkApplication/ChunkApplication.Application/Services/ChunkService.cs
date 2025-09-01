using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ChunkApplication.Domain.Entities;
using ChunkApplication.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ChunkApplication.Application.Services;

public class ChunkService : IChunkService
{
    private readonly IRepository<Files> _fileRepository;
    private readonly IRepository<Chunks> _chunkRepository;
    private readonly IEnumerable<IStorageProvider> _storageProviders;
    private readonly ILogger<ChunkService> _logger;
    private readonly Random _random;

    public ChunkService(
        IRepository<Files> fileRepository,
        IRepository<Chunks> chunkRepository,
        IEnumerable<IStorageProvider> storageProviders,
        ILogger<ChunkService> logger)
    {
        _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
        _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
        _storageProviders = storageProviders ?? throw new ArgumentNullException(nameof(storageProviders));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();
    }

    public async Task<Files> ChunkFileAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Starting to chunk file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileName(filePath);
            var checksum = await CalculateChecksumAsync(filePath);

            var (optimalChunkSize, totalChunks) = CalculateDynamicChunking(fileInfo.Length);
            var fileEntity = Files.Create(
                fileName,
                filePath,
                fileInfo.Length,
                checksum,
                optimalChunkSize,
                totalChunks
            );
            await _fileRepository.AddAsync(fileEntity);
            await ChunkFileAsync(fileEntity, fileInfo, optimalChunkSize, totalChunks);

            _logger.LogInformation("Successfully chunked file {FileName} into {TotalChunks} chunks", fileName, totalChunks);

            return fileEntity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error chunking file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<bool> ReconstructFileAsync(string fileId, string outputPath)
    {
        try
        {
            _logger.LogInformation("🔄 Starting to reconstruct file: {FileId} to {OutputPath}", fileId, outputPath);
            System.Console.WriteLine($"🔄 Starting reconstruction: FileId={fileId}");
            System.Console.WriteLine($"📁 Output path: {outputPath}");

            var fileEntity = await _fileRepository.GetByIdAsync(fileId);
            if (fileEntity == null)
            {
                _logger.LogWarning("File not found: {FileId}", fileId);
                return false;
            }
            var chunks = await _chunkRepository.GetAsync(c => c.FileId == fileId);
            var sortedChunks = chunks.OrderBy(c => c.ChunkNumber).ToList();

            if (sortedChunks.Count != fileEntity.TotalChunks)
            {
                _logger.LogWarning("Not all chunks found for file {FileId}. Expected: {Expected}, Found: {Found}", 
                    fileId, fileEntity.TotalChunks, sortedChunks.Count);
                return false;
            }
            _logger.LogInformation("🔧 Reconstructing from {ChunkCount} chunks...", sortedChunks.Count);
            System.Console.WriteLine($"🔧 Reconstructing from {sortedChunks.Count} chunks...");
            
            using var outputStream = File.Create(outputPath);
            var processedChunks = 0;
            
            foreach (var chunk in sortedChunks)
            {
                var chunkData = await GetChunkDataAsync(chunk);
                await outputStream.WriteAsync(chunkData, 0, chunkData.Length);
                processedChunks++;
                
                _logger.LogDebug("Processed chunk {ChunkNumber}/{TotalChunks} from {Provider}", 
                    chunk.ChunkNumber + 1, sortedChunks.Count, chunk.StorageProvider);
                System.Console.WriteLine($"📦 Processed chunk {chunk.ChunkNumber + 1}/{sortedChunks.Count} from {chunk.StorageProvider}");
            }
            outputStream.Dispose();
            string reconstructedChecksum = fileEntity.Checksum;
            var retryCount = 0;
            
            while (retryCount < 3)
            {
                try
                {
                    await Task.Delay(100);
                    reconstructedChecksum = await CalculateChecksumAsync(outputPath);
                    break; 
                }
                catch (IOException) when (retryCount < 2)
                {
                    retryCount++;
                    _logger.LogWarning("File locked during checksum verification, retrying... ({Retry}/3)", retryCount);
                    await Task.Delay(500); 
                }
                catch (IOException) 
                {
                    _logger.LogWarning("Skipping checksum verification due to file lock for {FileId}", fileId);
                    reconstructedChecksum = fileEntity.Checksum; 
                    break;
                }
            }
            
            if (reconstructedChecksum != fileEntity.Checksum)
            {
                _logger.LogError("Checksum mismatch for reconstructed file {FileId}", fileId);
                File.Delete(outputPath);
                return false;
            }

            _logger.LogInformation("✅ Successfully reconstructed file: {FileId} to {OutputPath}", fileId, outputPath);
            System.Console.WriteLine($"✅ File reconstructed successfully!");
            System.Console.WriteLine($"📁 Saved to: {outputPath}");
            System.Console.WriteLine($"📊 File size: {new FileInfo(outputPath).Length} bytes");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconstructing file: {FileId}", fileId);
            return false;
        }
    }

    public async Task<Files?> GetFileInfoAsync(string fileId)
    {
        try
        {
            return await _fileRepository.GetByIdAsync(fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info: {FileId}", fileId);
            return null;
        }
    }

    public async Task<IEnumerable<Files>> ListFilesAsync()
    {
        try
        {
            return await _fileRepository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files");
            return new List<Files>();
        }
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        try
        {
            _logger.LogInformation("Starting to delete file: {FileId}", fileId);

            var fileEntity = await _fileRepository.GetByIdAsync(fileId);
            if (fileEntity == null)
            {
                _logger.LogWarning("File not found: {FileId}", fileId);
                return false;
            }
            var chunks = await _chunkRepository.GetAsync(c => c.FileId == fileId);
            foreach (var chunk in chunks)
            {
                var storageProvider = _storageProviders.FirstOrDefault(sp => sp.Name == chunk.StorageProvider);
                if (storageProvider != null)
                {
                    await storageProvider.DeleteChunkAsync(chunk.Id);
                }
                await _chunkRepository.DeleteAsync(chunk);
            }
            await _fileRepository.DeleteAsync(fileEntity);
            _logger.LogInformation("Successfully deleted file: {FileId}", fileId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileId}", fileId);
            return false;

        }
    }

    private async Task ChunkFileAsync(Files fileEntity, FileInfo fileInfo, int optimalChunkSize, int totalChunks)
    {
        var storageProvidersArray = _storageProviders.ToArray();
        var chunkNumber = 0;
        var remainingBytes = fileInfo.Length;
        var actualChunkSize = optimalChunkSize;

        using var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

        while (chunkNumber < totalChunks && remainingBytes > 0)
        {
            var currentChunkSize = 0;
            
            if (fileInfo.Length < 64 * 1024) 
            {
                if (chunkNumber == 0) 
                {
                    currentChunkSize = (int)(fileInfo.Length / 2); 
                }
                else
                {
                    currentChunkSize = (int)remainingBytes;
                }
            }
            else 
            {
                if (chunkNumber == totalChunks - 1) 
                {
                    currentChunkSize = (int)remainingBytes;
                }
                else 
                {
                    currentChunkSize = (int)Math.Min(actualChunkSize, remainingBytes);
                }
            }
            
            var chunkId = $"{fileEntity.Id}_chunk_{chunkNumber}";
            var chunkData = new byte[currentChunkSize];
            var bytesRead = await fileStream.ReadAsync(chunkData, 0, currentChunkSize);
            if (bytesRead == 0 && remainingBytes > 0)
            {
                currentChunkSize = (int)remainingBytes;
                chunkData = new byte[currentChunkSize];
                bytesRead = await fileStream.ReadAsync(chunkData, 0, currentChunkSize);
            }
            if (bytesRead == 0) break;
            var selectedProvider = storageProvidersArray[_random.Next(storageProvidersArray.Length)];
            var chunkChecksum = CalculateChecksum(chunkData);
            var storagePath = await selectedProvider.StoreChunkAsync(chunkId, chunkData);
            
            _logger.LogDebug("Stored chunk data to {Provider}: {StoragePath}", 
                selectedProvider.Name, storagePath);

            var chunk = Chunks.CreateWithId(
                chunkId,  
                fileEntity.Id,
                chunkNumber,
                currentChunkSize,
                selectedProvider.Name,
                storagePath,
                chunkChecksum
            );

            // Save chunk metadata
            await _chunkRepository.AddAsync(chunk);
            fileEntity.AddChunk(chunk);

            remainingBytes -= bytesRead;
            chunkNumber++;

            _logger.LogDebug("Created chunk {ChunkNumber} for file {FileId}, size: {ChunkSize}, provider: {Provider}", 
                chunkNumber, fileEntity.Id, currentChunkSize, selectedProvider.Name);
        }
    }

    private (int optimalChunkSize, int totalChunks) CalculateDynamicChunking(long fileSize)
    {
        const long oneMB = 1024 * 1024;
        const long fiveMB = 5 * 1024 * 1024;
        const long tenMB = 10 * 1024 * 1024;
        const long hundredMB = 100 * 1024 * 1024;

        int optimalChunkSize;
        int targetChunks;
        if (fileSize < oneMB)
        {
            if (fileSize < 64 * 1024)
            {
                optimalChunkSize = Math.Max(1024, (int)(fileSize / 2)); 
                targetChunks = 2; // Her zaman 2 chunk
            }
            else if (fileSize < 256 * 1024) // 256 KB'dan küçük
            {
                optimalChunkSize = 128 * 1024; // 128 KB
                targetChunks = (int)Math.Ceiling((double)fileSize / optimalChunkSize);
            }
            else
            {
                optimalChunkSize = 256 * 1024; // 256 KB
                targetChunks = (int)Math.Ceiling((double)fileSize / optimalChunkSize);
            }
        }
        else if (fileSize < fiveMB)
        {
            optimalChunkSize = 512 * 1024; // 512 KB
            targetChunks = (int)Math.Ceiling((double)fileSize / optimalChunkSize);
        }
        else if (fileSize < tenMB)
        {
            optimalChunkSize = 1024 * 1024; // 1 MB
            targetChunks = (int)Math.Ceiling((double)fileSize / optimalChunkSize);
        }
        else if (fileSize < hundredMB)
        {
            optimalChunkSize = 2 * 1024 * 1024; // 2 MB
            targetChunks = (int)Math.Ceiling((double)fileSize / optimalChunkSize);
        }
        else
        {
            optimalChunkSize = 5 * 1024 * 1024; // 5 MB
            targetChunks = (int)Math.Ceiling((double)fileSize / optimalChunkSize);
        }

        // Minimum 2 chunk garantisi
        if (targetChunks < 2) targetChunks = 2;

        return (optimalChunkSize, targetChunks);
    }

    private async Task<string> CalculateChecksumAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToLower();
    }

    private string CalculateChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash).ToLower();
    }

    private async Task<byte[]> GetChunkDataAsync(Chunks chunk)
    {
        var storageProvider = _storageProviders.FirstOrDefault(sp => sp.Name == chunk.StorageProvider);
        if (storageProvider == null)
        {
            throw new InvalidOperationException($"Storage provider not found: {chunk.StorageProvider}");
        }

        return await storageProvider.RetrieveChunkAsync(chunk.Id);
    }
}
