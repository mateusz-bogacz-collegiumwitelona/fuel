using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Security.Cryptography.X509Certificates;

namespace Data.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Brand> Brand { get; set; }
        public DbSet<FuelPrice> FuelPrices { get; set; }
        public DbSet<FuelType> FuelTypes { get; set; }
        public DbSet<Logging> Logging { get; set; }
        public DbSet<PriceProposal> PriceProposals { get; set; }
        public DbSet<Station> Stations { get; set; }
        public DbSet<ProposalStatistic> ProposalStatistics { get; set; } 
        public DbSet<StationAddress> StationAddress { get; set; }
        public DbSet<BanRecord> BanRecords { get; set; }
        public DbSet<ReportUserRecord> ReportUserRecords { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            builder.Entity<Brand>()
                .HasIndex(b => b.Name)
                .IsUnique();

            builder.Entity<FuelType>()
                .HasIndex(ft => ft.Name)
                .IsUnique();

            builder.Entity<FuelType>()
                .HasIndex(ft => ft.Code)
                .IsUnique();

            builder.Entity<PriceProposal>()
               .Property(pp => pp.ProposedPrice)
               .HasPrecision(18, 2);

            builder.Entity<FuelPrice>()
                .Property(fp => fp.Price)
                .HasPrecision(18, 2);

            builder.Entity<ProposalStatistic>()
                .Property(ps => ps.AcceptedRate)
                .HasPrecision(5, 2);


            builder.Entity<ApplicationUser>()
                .HasOne(au => au.ProposalStatistic)
                .WithOne(ps => ps.User)
                .HasForeignKey<ProposalStatistic>(ps => ps.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasMany(u => u.PriceProposal)
                .WithOne(pp => pp.User)
                .HasForeignKey(pp => pp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Brand>()
                .HasMany(b => b.Station)
                .WithOne(s => s.Brand)
                .HasForeignKey(s => s.BrandId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Station>()
                .HasMany(s => s.PriceProposal)
                .WithOne(pp => pp.Station)
                .HasForeignKey(pp => pp.StationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Station>()
                .HasMany(s => s.FuelPrice)
                .WithOne(fp => fp.Station)
                .HasForeignKey(fp => fp.StationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FuelType>()
                .HasMany(ft => ft.PriceProposals)
                .WithOne(pp => pp.FuelType)
                .HasForeignKey(pp => pp.FuelTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FuelType>()
                .HasMany(ft => ft.FuelPrice)
                .WithOne(fp => fp.FuelType)
                .HasForeignKey(fp => fp.FuelTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Station>()
                .HasOne(s => s.Address)
                .WithOne(a => a.Station)
                .HasForeignKey<Station>(s => s.AddressId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BanRecord>()
            .HasOne(b => b.User)
            .WithMany(u => u.BansReceived)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict); 

            builder.Entity<BanRecord>()
                .HasOne(b => b.Admin)
                .WithMany(u => u.BansGiven)
                .HasForeignKey(b => b.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BanRecord>()
                .HasOne(b => b.UnbannedByAdmin)
                .WithMany(u => u.UnbansGiven)
                .HasForeignKey(b => b.UnbannedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReportUserRecord>()
                .HasOne(r => r.ReportingUser)
                .WithMany(u => u.ReportsMade)
                .HasForeignKey(r => r.ReportingUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReportUserRecord>()
                .HasOne(r => r.ReportedUser)
                .WithMany(u => u.ReportsReceived)
                .HasForeignKey(r => r.ReportedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReportUserRecord>()
                .HasOne(r => r.ReviewedByAdmin)
                .WithMany(u => u.ReportsReviewed)
                .HasForeignKey(r => r.ReviewedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StationAddress>()
                .Property(sa => sa.Location)
                .HasColumnType("geometry(Point, 4326)");
        }
    }
}
