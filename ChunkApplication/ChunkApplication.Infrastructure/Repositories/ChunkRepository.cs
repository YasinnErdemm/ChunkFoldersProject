using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ChunkApplication.Domain.Entities;
using ChunkApplication.Domain.Interfaces;
using ChunkApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChunkApplication.Infrastructure.Repositories;
public class ChunkRepository : Repository<Chunks>, IRepository<Chunks>
{
    public ChunkRepository(ChunkDbContext context) : base(context)
    {
    }

    public override async Task<Chunks?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or empty", nameof(id));

        return await _dbSet.FirstOrDefaultAsync(c => c.Id == id);
    }

    public override async Task<bool> ExistsAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or empty", nameof(id));

        return await _dbSet.AnyAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Chunks>> GetChunksByFileIdAsync(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("FileId cannot be null or empty", nameof(fileId));

        return await _dbSet
            .Where(c => c.FileId == fileId)
            .OrderBy(c => c.ChunkNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<Chunks>> GetChunksByStorageProviderAsync(string storageProvider)
    {
        if (string.IsNullOrWhiteSpace(storageProvider))
            throw new ArgumentException("StorageProvider cannot be null or empty", nameof(storageProvider));

        return await _dbSet
            .Where(c => c.StorageProvider == storageProvider)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetChunkCountByFileIdAsync(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("FileId cannot be null or empty", nameof(fileId));

        return await _dbSet.CountAsync(c => c.FileId == fileId);
    }

    public async Task<long> GetTotalChunkSizeByFileIdAsync(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("FileId cannot be null or empty", nameof(fileId));

        return await _dbSet
            .Where(c => c.FileId == fileId)
            .SumAsync(c => c.ChunkSize);
    }

    public async Task<IEnumerable<Chunks>> GetChunksByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}
