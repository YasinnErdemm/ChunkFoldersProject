using System;

namespace ChunkApplication.Domain.Entities;
public class Chunks
{
    public string Id { get; private set; }
    public string FileId { get; private set; }
    public int ChunkNumber { get; private set; }
    public int ChunkSize { get; private set; }
    public string StorageProvider { get; private set; }
    public string Checksum { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Chunks() { }

    public static Chunks Create(
        string fileId,
        int chunkNumber,
        int chunkSize,
        string storageProvider,
        string checksum)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("FileId cannot be null or empty", nameof(fileId));
            
        if (chunkNumber < 0)
            throw new ArgumentException("ChunkNumber cannot be negative", nameof(chunkNumber));
            
        if (chunkSize <= 0)
            throw new ArgumentException("ChunkSize must be greater than 0", nameof(chunkSize));
            
        if (string.IsNullOrWhiteSpace(storageProvider))
            throw new ArgumentException("StorageProvider cannot be null or empty", nameof(storageProvider));
            
            
        if (string.IsNullOrWhiteSpace(checksum))
            throw new ArgumentException("Checksum cannot be null or empty", nameof(checksum));

        return new Chunks
        {
            Id = Guid.NewGuid().ToString("N"),
            FileId = fileId,
            ChunkNumber = chunkNumber,
            ChunkSize = chunkSize,
            StorageProvider = storageProvider,
            Checksum = checksum,
            CreatedAt = DateTime.UtcNow
        };
    }
    public static Chunks CreateWithId(
        string id,
        string fileId,
        int chunkNumber,
        int chunkSize,
        string storageProvider,
        string storagePath,
        string checksum)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or empty", nameof(id));
            
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("FileId cannot be null or empty", nameof(fileId));
            
        if (chunkNumber < 0)
            throw new ArgumentException("ChunkNumber cannot be negative", nameof(chunkNumber));
            
        if (chunkSize <= 0)
            throw new ArgumentException("ChunkSize must be greater than 0", nameof(chunkSize));
            
        if (string.IsNullOrWhiteSpace(storageProvider))
            throw new ArgumentException("StorageProvider cannot be null or empty", nameof(storageProvider));
            
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("StoragePath cannot be null or empty", nameof(storagePath));
            
        if (string.IsNullOrWhiteSpace(checksum))
            throw new ArgumentException("Checksum cannot be null or empty", nameof(checksum));

        return new Chunks
        {
            Id = id,  
            FileId = fileId,
            ChunkNumber = chunkNumber,
            ChunkSize = chunkSize,
            StorageProvider = storageProvider,
            Checksum = checksum,
            CreatedAt = DateTime.UtcNow
        };
    }
    public bool BelongsToFile(string fileId)
    {
        return FileId == fileId;
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Id) &&
               !string.IsNullOrWhiteSpace(FileId) &&
               ChunkNumber >= 0 &&
               ChunkSize > 0 &&
               !string.IsNullOrWhiteSpace(StorageProvider) &&
               !string.IsNullOrWhiteSpace(Checksum);
    }
}
