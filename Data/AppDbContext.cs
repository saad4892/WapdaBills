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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlogPost>().HasIndex(post => post.Slug).IsUnique();
        modelBuilder.Entity<JobListing>().Property(job => job.ApplyUrl).HasMaxLength(500);
    }
}
