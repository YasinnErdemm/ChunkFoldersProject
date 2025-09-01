namespace ChunkClient.Messages.Requests;

/// <summary>
/// Request message for reconstructing a file
/// </summary>
public class ReconstructFileMessage
{
    public string FileId { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
