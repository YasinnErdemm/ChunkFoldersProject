namespace ChunkApplication.Messages.Requests;

public class GetFileInfoMessage
{
    public string FileId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
