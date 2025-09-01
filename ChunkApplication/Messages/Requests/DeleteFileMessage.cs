namespace ChunkApplication.Messages.Requests;

public class DeleteFileMessage
{
    public string FileId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
