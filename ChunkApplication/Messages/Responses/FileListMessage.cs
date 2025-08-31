namespace ChunkApplication.Messages.Responses;

/// <summary>
/// Response message for individual file information
/// </summary>
public class FileListMessage
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int TotalChunks { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RequestId { get; set; } = string.Empty;
}
