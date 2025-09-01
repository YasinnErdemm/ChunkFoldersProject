using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChunkApplication.ChunkApplication.Infrastructure.MessageHandlers.Models
{
    public class GetFileInfoRequest
    {
        public string RequestId { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
    public class GetFileInfoResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int TotalChunks { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsComplete { get; set; }
        public bool Integrity { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
    public class FileInfo
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int TotalChunks { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsComplete { get; set; }
    }
    public class FileListResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public List<FileInfo> Files { get; set; } = new();
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
