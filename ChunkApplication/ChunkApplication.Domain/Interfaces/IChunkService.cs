using System.Threading.Tasks;
using ChunkApplication.Domain.Entities;

namespace ChunkApplication.Domain.Interfaces;


public interface IChunkService
{

    Task<Files> ChunkFileAsync(string filePath);
    
    Task<bool> ReconstructFileAsync(string fileId, string outputPath);
    
    Task<Files?> GetFileInfoAsync(string fileId);
    
    Task<IEnumerable<Files>> ListFilesAsync();
    
    Task<bool> DeleteFileAsync(string fileId);
}
