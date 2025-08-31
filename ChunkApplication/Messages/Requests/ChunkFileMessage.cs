namespace ChunkApplication.Messages.Requests;

/// <summary>
/// Request message for chunking a file
/// </summary>
public class ChunkFileMessage
{
    public string FilePath { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
