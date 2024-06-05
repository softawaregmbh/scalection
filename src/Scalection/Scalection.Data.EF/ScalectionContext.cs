using Microsoft.EntityFrameworkCore;
using Scalection.Data.EF.Models;

namespace Scalection.Data.EF
{
    public class ScalectionContext : DbContext
    {
        public DbSet<Election> Elections { get; set; }
        public DbSet<Party> Parties { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<ElectionDistrict> ElectionDistricts { get; set; }
        public DbSet<Voter> Voters { get; set; }
        public DbSet<Vote> Votes { get; set; }

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
            modelBuilder.Entity<Party>()
                .HasMany(e => e.Candidates)
                .WithOne()
                .HasForeignKey(e => e.PartyId);

            modelBuilder.Entity<Candidate>().HasKey(e => e.CandidateId);

            modelBuilder.Entity<ElectionDistrict>().HasKey(e => e.ElectionDistrictId);
            modelBuilder.Entity<ElectionDistrict>().Property(e => e.ElectionDistrictId).ValueGeneratedNever();
            modelBuilder.Entity<ElectionDistrict>()
                .HasOne<Election>()
                .WithMany()
                .HasForeignKey(e => e.ElectionId);

            modelBuilder.Entity<Voter>().HasKey(e => e.VoterId);
            modelBuilder.Entity<Voter>().Property(e => e.VoterId).ValueGeneratedNever();
            modelBuilder.Entity<Voter>()
                .HasOne<ElectionDistrict>()
                .WithMany()
                .HasForeignKey(e => e.ElectionDistrictId);

            modelBuilder.Entity<Vote>().HasKey(e => e.VoteId);
            modelBuilder.Entity<Vote>()
                .HasOne<ElectionDistrict>()
                .WithMany()
                .HasForeignKey(e => e.ElectionDistrictId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Vote>()
                .HasOne<Party>()
                .WithMany()
                .HasForeignKey(e => e.PartyId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Vote>()
                .HasOne<Candidate>()
                .WithMany()
                .HasForeignKey(e => e.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
