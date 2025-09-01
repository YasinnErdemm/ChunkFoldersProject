using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChunkApplication.ChunkApplication.Infrastructure.MessageHandlers.Models
{
    public class ReconstructFileRequest
    {
        public string RequestId { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
    public class ReconstructFileResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
