namespace ChunkApplication.Interfaces;

using ChunkApplication.Models;

/// <summary>
/// Defines the contract for chunking operations
/// </summary>
public interface IChunkService
{
    /// <summary>
    /// Splits a file into chunks and distributes them across storage providers
    /// </summary>
    /// <param name="filePath">Path to the file to be chunked</param>
    /// <returns>File metadata containing chunk information</returns>
    Task<FileMetadata> ChunkFileAsync(string filePath);
    
    /// <summary>
    /// Reconstructs a file from its chunks
    /// </summary>
    /// <param name="fileId">Unique identifier of the file</param>
    /// <param name="outputPath">Path where the reconstructed file will be saved</param>
    /// <returns>True if reconstruction was successful, false otherwise</returns>
    Task<bool> ReconstructFileAsync(string fileId, string outputPath);
    
    /// <summary>
    /// Gets information about a chunked file
    /// </summary>
    /// <param name="fileId">Unique identifier of the file</param>
    /// <returns>File metadata or null if not found</returns>
    Task<FileMetadata?> GetFileMetadataAsync(string fileId);
    
    /// <summary>
    /// Lists all chunked files in the system
    /// </summary>
    /// <returns>Collection of file metadata</returns>
    Task<IEnumerable<FileMetadata>> ListFilesAsync();
    
    /// <summary>
    /// Deletes a chunked file and all its chunks
    /// </summary>
    /// <param name="fileId">Unique identifier of the file</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> DeleteFileAsync(string fileId);
}
