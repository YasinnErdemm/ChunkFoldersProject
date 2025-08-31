namespace ChunkClient.Messages.Requests;

/// <summary>
/// Request message for listing files
/// </summary>
public class ListFilesMessage
{
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
