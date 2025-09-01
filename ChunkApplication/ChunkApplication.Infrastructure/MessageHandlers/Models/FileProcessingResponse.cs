namespace ChunkApplication.Infrastructure.MessageHandlers.Models;

public class FileProcessingResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalChunks { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public long ProcessingTimeMs { get; set; }
}
