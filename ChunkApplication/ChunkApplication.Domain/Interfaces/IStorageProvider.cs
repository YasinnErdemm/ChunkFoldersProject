using System.Threading.Tasks;

namespace ChunkApplication.Domain.Interfaces;

/// <summary>
/// Interface for storage providers
/// </summary>
public interface IStorageProvider
{
    string Name { get; }
    Task<string> StoreChunkAsync(string chunkId, byte[] data);
    Task<byte[]> RetrieveChunkAsync(string chunkId);
    Task DeleteChunkAsync(string chunkId);
    Task<bool> ChunkExistsAsync(string chunkId);
}
