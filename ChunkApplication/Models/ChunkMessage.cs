using System.ComponentModel.DataAnnotations;

namespace ChunkApplication.Models;

/// <summary>
/// Message model for chunk processing via RabbitMQ
/// </summary>
public class ChunkMessage
{
    [Required]
    public string FileId { get; set; } = string.Empty;
    
    [Required]
    public string ChunkId { get; set; } = string.Empty;
    
    [Required]
    public byte[] ChunkData { get; set; } = Array.Empty<byte>();
    
    [Required]
    public int ChunkNumber { get; set; }
    
    [Required]
    public string StorageProvider { get; set; } = string.Empty;
    
    [Required]
    public string Checksum { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
