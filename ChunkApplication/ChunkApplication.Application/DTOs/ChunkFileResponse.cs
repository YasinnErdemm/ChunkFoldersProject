namespace ChunkApplication.Application.DTOs;

public class ChunkFileResponse
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalChunks { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
