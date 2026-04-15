using Microsoft.EntityFrameworkCore;
using IdentitiyService.Models;

namespace IdentitiyService.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<PasswordAuth> PasswordAuths { get; set; } = null!;
    public DbSet<GoogleAuth> GoogleAuths { get; set; } = null!;
    public DbSet<Profile> Profiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(u => u.PasswordAuth)
            .WithOne(pa => pa.User)
            .HasForeignKey<PasswordAuth>(pa => pa.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.GoogleAuth)
            .WithOne(ga => ga.User)
            .HasForeignKey<GoogleAuth>(ga => ga.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<Profile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}