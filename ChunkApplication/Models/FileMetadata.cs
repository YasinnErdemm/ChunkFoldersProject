namespace ChunkApplication.Models;

/// <summary>
/// Represents metadata for a chunked file
/// </summary>
public class FileMetadata
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessed { get; set; }
    public int TotalChunks { get; set; }
    public int ChunkSize { get; set; }
    public List<ChunkInfo> Chunks { get; set; } = new();
}

/// <summary>
/// Represents information about a single chunk
/// </summary>
public class ChunkInfo
{
    public string Id { get; set; } = string.Empty;
    public string FileId { get; set; } = string.Empty;
    public int ChunkNumber { get; set; }
    public int ChunkSize { get; set; }
    public string StorageProvider { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
