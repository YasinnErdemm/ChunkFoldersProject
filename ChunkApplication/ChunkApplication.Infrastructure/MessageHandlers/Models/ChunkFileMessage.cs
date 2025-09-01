namespace ChunkApplication.Infrastructure.MessageHandlers.Models;

public class ChunkFileMessage
{
    public string FilePath { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
