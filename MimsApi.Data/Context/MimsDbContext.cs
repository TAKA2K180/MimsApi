using Microsoft.EntityFrameworkCore;
using MimsApi.Core.models;

namespace MimsApi.Data.Context
{
    public class MimsDbContext : DbContext
    {
        public MimsDbContext(DbContextOptions<MimsDbContext> options) : base(options)
        {
        }

        public DbSet<Products> Products { get; set; }
        public DbSet<PackagingHeader> PackagingHeaders { get; set; }
        public DbSet<PackagingDetail> PackagingDetails { get; set; }
        public DbSet<Items> Items { get; set; }
        public DbSet<Users> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Products entity
            modelBuilder.Entity<Products>(entity =>
            {
                entity.ToTable("Products");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");
                
                entity.HasMany(e => e.PackagingHeaders)
                    .WithOne(e => e.Product)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure PackagingHeader entity
            modelBuilder.Entity<PackagingHeader>(entity =>
            {
                entity.ToTable("PackagingHeaders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.PackagingName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");
                entity.Property(e => e.PackagingType)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                // Relationship to Product
                entity.HasOne(e => e.Product)
                    .WithMany(e => e.PackagingHeaders)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Self-referential relationship for hierarchical packaging
                entity.HasOne(e => e.ParentPackaging)
                    .WithMany(e => e.ChildPackagings)
                    .HasForeignKey(e => e.ParentPackagingId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationship to PackagingDetail
                entity.HasMany(e => e.PackagingDetails)
                    .WithOne(e => e.PackagingHeader)
                    .HasForeignKey(e => e.PackagingHeaderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Create index for ProductId
                entity.HasIndex(e => e.ProductId)
                    .HasName("IX_PackagingHeaders_ProductId");

                // Create index for ParentPackagingId
                entity.HasIndex(e => e.ParentPackagingId)
                    .HasName("IX_PackagingHeaders_ParentPackagingId");
            });

            // Configure PackagingDetail entity
            modelBuilder.Entity<PackagingDetail>(entity =>
            {
                entity.ToTable("PackagingDetails");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.Quantity)
                    .IsRequired()
                    .HasColumnType("int");

                // Relationship to PackagingHeader
                entity.HasOne(e => e.PackagingHeader)
                    .WithMany(e => e.PackagingDetails)
                    .HasForeignKey(e => e.PackagingHeaderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship to Items
                entity.HasOne(e => e.Items)
                    .WithMany(e => e.PackagingDetails)
                    .HasForeignKey(e => e.ItemsId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Create indexes for foreign keys
                entity.HasIndex(e => e.PackagingHeaderId)
                    .HasName("IX_PackagingDetails_PackagingHeaderId");

                entity.HasIndex(e => e.ItemsId)
                    .HasName("IX_PackagingDetails_ItemsId");
            });

            // Configure Items entity
            modelBuilder.Entity<Items>(entity =>
            {
                entity.ToTable("Items");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.ItemName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");
                
                entity.HasMany(e => e.PackagingDetails)
                    .WithOne(e => e.Items)
                    .HasForeignKey(e => e.ItemsId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Users entity
            modelBuilder.Entity<Users>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");
                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)");
                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");
                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.LastLogin)
                    .HasColumnType("datetime2");

                // Create unique index on Username
                entity.HasIndex(e => e.Username)
                    .IsUnique()
                    .HasName("IX_Users_Username_Unique");

                // Create unique index on Email
                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasName("IX_Users_Email_Unique");
            });
        }
    }
}
