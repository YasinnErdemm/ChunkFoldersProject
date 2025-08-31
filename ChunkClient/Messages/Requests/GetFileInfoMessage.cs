namespace ChunkClient.Messages.Requests;

/// <summary>
/// Request message for getting file information
/// </summary>
public class GetFileInfoMessage
{
    public string FileId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
