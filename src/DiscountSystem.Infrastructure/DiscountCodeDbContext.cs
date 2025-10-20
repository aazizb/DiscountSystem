using DiscountSystem.Domain.Models;

using Microsoft.EntityFrameworkCore;

namespace DiscountSystem.Infrastructure
{
    public class DiscountCodeDbContext : DbContext
    {
        public DiscountCodeDbContext(DbContextOptions<DiscountCodeDbContext> options)
            : base(options)
        {
        }
        public DbSet<DiscountCode> DiscountCodes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            const int maxCodeLength = 8;
            modelBuilder.Entity<DiscountCode>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Ensure codes are unique and fast to look up
                entity.HasIndex(e => e.Code).IsUnique();
                // Configure code max length and required properties
                entity.Property(e => e.Code).IsRequired().HasMaxLength(maxCodeLength);
                entity.Property(e => e.IsUsed).IsRequired();

                // Configures as concurrency token
                entity.Property(e => e.RowVersion).IsRowVersion();
            });

            base.OnModelCreating(modelBuilder);
        }
    }

}
