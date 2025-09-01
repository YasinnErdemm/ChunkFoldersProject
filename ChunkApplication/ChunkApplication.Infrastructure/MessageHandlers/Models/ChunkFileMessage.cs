namespace ChunkApplication.Infrastructure.MessageHandlers.Models;

public class ChunkFileMessage
{
    public string FilePath { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
public class DeleteFileResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string FileId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
