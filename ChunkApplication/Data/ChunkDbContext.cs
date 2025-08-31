using Microsoft.EntityFrameworkCore;
using ChunkApplication.Models;

namespace ChunkApplication.Data;

/// <summary>
/// Entity Framework DbContext for chunk application
/// </summary>
public class ChunkDbContext : DbContext
{
    public ChunkDbContext(DbContextOptions<ChunkDbContext> options) : base(options)
    {
    }

    public DbSet<FileMetadata> Files { get; set; }
    public DbSet<ChunkInfo> Chunks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure FileMetadata entity
        modelBuilder.Entity<FileMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.OriginalPath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Checksum).HasMaxLength(64).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Configure relationship with ChunkInfo
            entity.HasMany(e => e.Chunks)
                  .WithOne()
                  .HasForeignKey(c => c.FileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ChunkInfo entity
        modelBuilder.Entity<ChunkInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.FileId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.StorageProvider).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Checksum).HasMaxLength(64).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Create index on FileId for better performance
            entity.HasIndex(e => e.FileId);
        });
    }
}
