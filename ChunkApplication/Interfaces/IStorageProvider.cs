namespace ChunkApplication.Interfaces;

/// <summary>
/// Defines the contract for different storage providers
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// Stores a chunk of data with the specified identifier
    /// </summary>
    /// <param name="chunkId">Unique identifier for the chunk</param>
    /// <param name="data">Chunk data to store</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task StoreChunkAsync(string chunkId, byte[] data);
    
    /// <summary>
    /// Retrieves a chunk of data by its identifier
    /// </summary>
    /// <param name="chunkId">Unique identifier for the chunk</param>
    /// <returns>Chunk data as byte array</returns>
    Task<byte[]> RetrieveChunkAsync(string chunkId);
    
    /// <summary>
    /// Deletes a chunk by its identifier
    /// </summary>
    /// <param name="chunkId">Unique identifier for the chunk</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task DeleteChunkAsync(string chunkId);
    
    /// <summary>
    /// Checks if a chunk exists in the storage
    /// </summary>
    /// <param name="chunkId">Unique identifier for the chunk</param>
    /// <returns>True if chunk exists, false otherwise</returns>
    Task<bool> ChunkExistsAsync(string chunkId);
    
    /// <summary>
    /// Gets the name/type of the storage provider
    /// </summary>
    string ProviderName { get; }
}
