using Engines;
using Localization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StrengthOfMaterials;
namespace UserManagement
{
    public class DataContext : IdentityDbContext<AppUser>
    {
        public static Authentication.Type AuthType = Authentication.Type.Session;
        public DataContext(DbContextOptions<DataContext> options)
        : base(options)
        {            
        }

        public DbSet<Motorok> Engines { get; set; }
        public DbSet<MaterialStrength> MaterialStrength { get; set; }
        public DbSet<Point> Point { get; set; }
        public DbSet<LocalizationEntry> LocalizationEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SubRegion>()
                .HasOne(s => s.CutoutRegionOwner)
                .WithMany(m => m.CutoutRegions)
                .HasForeignKey(s => s.CutoutRegionOwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SubRegion>()
                .HasOne(s => s.AddedRegionOwner)
                .WithMany(m => m.AddedRegions)
                .HasForeignKey(s => s.AddedRegionOwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Motorok>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Id)
                    .ValueGeneratedOnAdd();
            });
        }
    }
}
