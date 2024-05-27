using Microsoft.EntityFrameworkCore;
using Scalection.ApiService.Models;

namespace Scalection.ApiService
{
    public class ScalectionContext : DbContext
    {
        public DbSet<Election> Elections { get; set; }
        public DbSet<Party> Parties { get; set; }

        public ScalectionContext(DbContextOptions<ScalectionContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Election>().HasKey(e => e.ElectionId);

            modelBuilder.Entity<Party>().HasKey(e => e.PartyId);
            modelBuilder.Entity<Party>()
                .HasOne(e => e.Election)
                .WithMany()
                .HasForeignKey(e => e.ElectionId);
        }
    }
}
