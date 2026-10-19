using Microsoft.EntityFrameworkCore;
using WapdaBill.Models;

namespace WapdaBill.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();

    public DbSet<JobListing> JobListings => Set<JobListing>();

    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlogPost>().HasIndex(post => post.Slug).IsUnique();
        modelBuilder.Entity<JobListing>().Property(job => job.ApplyUrl).HasMaxLength(500);
        modelBuilder.Entity<ContactMessage>(entity =>
        {
            entity.Property(m => m.Name).HasMaxLength(80);
            entity.Property(m => m.Email).HasMaxLength(160);
            entity.Property(m => m.Message).HasMaxLength(2000);
            entity.Property(m => m.UserAgent).HasMaxLength(300);
        });
    }
}
