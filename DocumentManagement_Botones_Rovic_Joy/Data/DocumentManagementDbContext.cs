using DocumentManagement_Botones_Rovic_Joy.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagement_Botones_Rovic_Joy.Data;

public sealed class DocumentManagementDbContext(DbContextOptions<DocumentManagementDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentMetadata> DocumentMetadata => Set<DocumentMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(document => document.DocumentId);
            entity.Property(document => document.StoredFileName).HasMaxLength(100).IsRequired();
            entity.Property(document => document.FileSize).IsRequired();
            entity.Property(document => document.UploadedAt).IsRequired();
            entity.HasOne(document => document.Metadata)
                .WithOne(metadata => metadata.Document)
                .HasForeignKey<DocumentMetadata>(metadata => metadata.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentMetadata>(entity =>
        {
            entity.ToTable("DocumentMetadata");
            entity.HasKey(metadata => metadata.DocumentId);
            entity.Property(metadata => metadata.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(metadata => metadata.ContentType).HasMaxLength(127).IsRequired();
        });
    }
}
