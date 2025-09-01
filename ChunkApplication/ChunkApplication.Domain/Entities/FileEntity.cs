using System;
using System.Collections.Generic;
using System.Linq;

namespace ChunkApplication.Domain.Entities;
public class Files
{
    public string Id { get; private set; }
    public string FileName { get; private set; }
    public string OriginalPath { get; private set; }
    public long FileSize { get; private set; }
    public string Checksum { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastAccessed { get; private set; }
    public int ChunkSize { get; private set; }
    public int TotalChunks { get; private set; }
    
    public ICollection<Chunks> Chunks { get; private set; } = new List<Chunks>();
    private Files() { }

    public static Files Create(
        string fileName, 
        string originalPath, 
        long fileSize, 
        string checksum, 
        int chunkSize, 
        int totalChunks)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName cannot be null or empty", nameof(fileName));
            
        if (string.IsNullOrWhiteSpace(originalPath))
            throw new ArgumentException("OriginalPath cannot be null or empty", nameof(originalPath));
            
        if (fileSize <= 0)
            throw new ArgumentException("FileSize must be greater than 0", nameof(fileSize));
            
        if (string.IsNullOrWhiteSpace(checksum))
            throw new ArgumentException("Checksum cannot be null or empty", nameof(checksum));
            
        if (chunkSize <= 0)
            throw new ArgumentException("ChunkSize must be greater than 0", nameof(chunkSize));
            
        if (totalChunks <= 0)
            throw new ArgumentException("TotalChunks must be greater than 0", nameof(totalChunks));

        return new Files
        {
            Id = Guid.NewGuid().ToString("N"),
            FileName = fileName,
            OriginalPath = originalPath,
            FileSize = fileSize,
            Checksum = checksum,
            CreatedAt = DateTime.UtcNow,
            ChunkSize = chunkSize,
            TotalChunks = totalChunks
        };
    }
    public void AddChunk(Chunks chunk)
    {
        if (chunk == null)
            throw new ArgumentNullException(nameof(chunk));
            
        if (chunk.FileId != Id)
            throw new InvalidOperationException("Chunk does not belong to this file");
            
        Chunks.Add(chunk);
    }

    public void UpdateLastAccessed()
    {
        LastAccessed = DateTime.UtcNow;
    }

    public bool IsComplete()
    {
        return Chunks.Count == TotalChunks;
    }

    public long GetTotalChunkSize()
    {
        return Chunks.Sum(c => c.ChunkSize);
    }

    public bool ValidateIntegrity()
    {
        return Chunks.Count == TotalChunks && 
               GetTotalChunkSize() == FileSize;
    }
}
