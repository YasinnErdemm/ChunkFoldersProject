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

public class FileRepository : Repository<Files>, IRepository<Files>
{
    public FileRepository(ChunkDbContext context) : base(context)
    {
    }

    public override async Task<Files?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or empty", nameof(id));

        return await _dbSet
            .Include(f => f.Chunks)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public override async Task<bool> ExistsAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or empty", nameof(id));

        return await _dbSet.AnyAsync(f => f.Id == id);
    }

    public async Task<Files?> GetByFileNameAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName cannot be null or empty", nameof(fileName));

        return await _dbSet
            .Include(f => f.Chunks)
            .FirstOrDefaultAsync(f => f.FileName == fileName);
    }

    public async Task<IEnumerable<Files>> GetFilesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(f => f.Chunks)
            .Where(f => f.CreatedAt >= startDate && f.CreatedAt <= endDate)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Files>> GetFilesBySizeRangeAsync(long minSize, long maxSize)
    {
        return await _dbSet
            .Include(f => f.Chunks)
            .Where(f => f.FileSize >= minSize && f.FileSize <= maxSize)
            .OrderByDescending(f => f.FileSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalFileCountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public async Task<long> GetTotalStorageUsedAsync()
    {
        return await _dbSet.SumAsync(f => f.FileSize);
    }
}
