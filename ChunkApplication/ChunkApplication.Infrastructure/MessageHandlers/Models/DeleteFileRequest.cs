using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChunkApplication.ChunkApplication.Infrastructure.MessageHandlers.Models
{
    public class DeleteFileRequest
    {
        public string RequestId { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
