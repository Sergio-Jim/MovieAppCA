using Microsoft.EntityFrameworkCore;
using MovieApp.Domain.Entities;

namespace MovieApp.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Movie> Movies { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(entity =>
            {
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            builder.Entity<Movie>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(60);
                entity.Property(e => e.Genre).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Rating).IsRequired().HasMaxLength(5);
                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            });
        }
    }
}
