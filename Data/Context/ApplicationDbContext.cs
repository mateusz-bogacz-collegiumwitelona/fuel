using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Models;

namespace Data.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        DbSet<ApplicationUser> ApplicationUsers { get; set; }
        DbSet<Brand> Brands { get; set; }
        DbSet<FuelPrice> FuelPrices { get; set; }
        DbSet<FuelType> FuelTypes { get; set; }
        DbSet<Logging> Logging { get; set; }
        DbSet<PriceProposal> PriceProposals { get; set; }
        DbSet<Station> Stations { get; set; }

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

            builder.Entity<ProposalStatisict>()
                .Property(ps => ps.ProposedPrice)
                .HasPrecision(18, 2);

            builder.Entity<ProposalStatisict>()
                .Property(ps => ps.AcceptedRate)
                .HasPrecision(5, 2);


            builder.Entity<ApplicationUser>()
                .HasOne(au => au.ProposalStatisict)
                .WithOne(ps => ps.User)
                .HasForeignKey<ProposalStatisict>(ps => ps.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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
                .Property(s => s.Location)
                .HasColumnType("geometry(Point, 4326)");
        }
    }
}
