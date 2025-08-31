namespace ChunkApplication.Messages.Requests;

/// <summary>
/// Request message for deleting a file
/// </summary>
public class DeleteFileMessage
{
    public string FileId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
