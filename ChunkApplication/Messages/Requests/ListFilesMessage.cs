namespace ChunkApplication.Messages.Requests;

public class ListFilesMessage
{
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
