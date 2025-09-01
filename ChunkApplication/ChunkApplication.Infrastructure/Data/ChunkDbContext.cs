using Microsoft.EntityFrameworkCore;
using ChunkApplication.Domain.Entities;

namespace ChunkApplication.Infrastructure.Data;
public class ChunkDbContext : DbContext
{
    public ChunkDbContext(DbContextOptions<ChunkDbContext> options) : base(options)
    {
    }

    public DbSet<Files> Files { get; set; }
    public DbSet<Chunks> Chunks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Files>(entity =>
        {
            entity.ToTable("Files");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(32);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.OriginalPath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Checksum).IsRequired().HasMaxLength(64);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastAccessed);
            entity.Property(e => e.ChunkSize).IsRequired();
            entity.Property(e => e.TotalChunks).IsRequired();

            entity.HasMany(e => e.Chunks)
                  .WithOne()
                  .HasForeignKey(c => c.FileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Chunks>(entity =>
        {
            entity.ToTable("Chunks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(32);
            entity.Property(e => e.FileId).IsRequired().HasMaxLength(32);
            entity.Property(e => e.ChunkNumber).IsRequired();
            entity.Property(e => e.ChunkSize).IsRequired();
            entity.Property(e => e.StorageProvider).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Checksum).IsRequired().HasMaxLength(64);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.FileId);
            entity.HasIndex(e => e.ChunkNumber);
            entity.HasIndex(e => e.StorageProvider);
        });
    }
}
