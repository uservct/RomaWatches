using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Models;

namespace RomaWatches.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Product entity
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(e => e.Brand);
                entity.HasIndex(e => e.Price);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Configure Cart entity
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure CartItem entity
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasIndex(e => e.CartId);
                entity.HasIndex(e => e.ProductId);
                entity.HasOne(ci => ci.Cart)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ci => ci.Product)
                    .WithMany()
                    .HasForeignKey(ci => ci.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
