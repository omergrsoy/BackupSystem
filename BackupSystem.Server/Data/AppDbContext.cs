using BackupSystem.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace BackupSystem.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Machine> Machines { get; set; }
        public DbSet<BackupRecord> Backups { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Machine>()
                .HasMany(m => m.Backups)
                .WithOne(b => b.Machine)
                .HasForeignKey(b => b.MachineId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
