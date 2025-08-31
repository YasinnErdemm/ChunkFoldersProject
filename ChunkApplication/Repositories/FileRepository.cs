using Microsoft.EntityFrameworkCore;
using ChunkApplication.Data;
using ChunkApplication.Interfaces;
using ChunkApplication.Models;

namespace ChunkApplication.Repositories;

/// <summary>
/// Repository for file metadata operations
/// </summary>
public class FileRepository : Repository<FileMetadata>, IFileRepository
{
    public FileRepository(ChunkDbContext context) : base(context)
    {
    }

    public override async Task<FileMetadata?> GetByIdAsync(string id)
    {
        return await _context.Files
            .Include(f => f.Chunks)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public override async Task<IEnumerable<FileMetadata>> GetAllAsync()
    {
        return await _context.Files
            .Include(f => f.Chunks)
            .ToListAsync();
    }

    public async Task<FileMetadata?> GetByFileNameAsync(string fileName)
    {
        return await _context.Files
            .Include(f => f.Chunks)
            .FirstOrDefaultAsync(f => f.FileName == fileName);
    }

    public async Task<IEnumerable<FileMetadata>> GetFilesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Files
            .Include(f => f.Chunks)
            .Where(f => f.CreatedAt >= startDate && f.CreatedAt <= endDate)
            .ToListAsync();
    }
}

/// <summary>
/// Interface for file repository operations
/// </summary>
public interface IFileRepository : IRepository<FileMetadata>
{
    Task<FileMetadata?> GetByFileNameAsync(string fileName);
    Task<IEnumerable<FileMetadata>> GetFilesByDateRangeAsync(DateTime startDate, DateTime endDate);
}
