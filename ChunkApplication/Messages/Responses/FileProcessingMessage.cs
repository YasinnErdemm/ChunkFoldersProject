namespace ChunkApplication.Messages.Responses;

/// <summary>
/// Response message for file processing status
/// </summary>
public class FileProcessingMessage
{
    public string FileId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
