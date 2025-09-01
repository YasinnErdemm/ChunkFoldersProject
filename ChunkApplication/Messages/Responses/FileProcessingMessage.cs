namespace ChunkApplication.Messages.Responses;

public class FileProcessingMessage
{
    public string FileId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
