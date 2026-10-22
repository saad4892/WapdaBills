using Microsoft.EntityFrameworkCore;
using WapdaBill.Models;
using WapdaBill.Models.Cms;

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

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<NavMenu> NavMenus => Set<NavMenu>();
    public DbSet<NavItem> NavItems => Set<NavItem>();
    public DbSet<FooterColumnEntity> FooterColumns => Set<FooterColumnEntity>();
    public DbSet<FooterLinkEntity> FooterLinks => Set<FooterLinkEntity>();
    public DbSet<SocialLinkEntity> SocialLinks => Set<SocialLinkEntity>();
    public DbSet<FooterCopy> FooterCopies => Set<FooterCopy>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<DiscoEntity> Discos => Set<DiscoEntity>();
    public DbSet<RegionHubEntity> RegionHubs => Set<RegionHubEntity>();
    public DbSet<HomepageSection> HomepageSections => Set<HomepageSection>();
    public DbSet<HomepageItem> HomepageItems => Set<HomepageItem>();
    public DbSet<FaqEntity> Faqs => Set<FaqEntity>();
    public DbSet<RedirectRule> RedirectRules => Set<RedirectRule>();
    public DbSet<NotFoundLog> NotFoundLogs => Set<NotFoundLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<GuideFaq> GuideFaqs => Set<GuideFaq>();

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

        modelBuilder.Entity<AdminUser>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<ContentPage>().HasIndex(p => p.Slug).IsUnique();
        modelBuilder.Entity<DiscoEntity>().HasIndex(d => d.Code).IsUnique();
        modelBuilder.Entity<DiscoEntity>().HasIndex(d => d.PublicSlug).IsUnique();
        modelBuilder.Entity<RegionHubEntity>().HasIndex(r => r.Path).IsUnique();
        modelBuilder.Entity<NavMenu>().HasIndex(m => m.Key).IsUnique();
        modelBuilder.Entity<HomepageSection>().HasIndex(s => s.Key).IsUnique();
        modelBuilder.Entity<RedirectRule>().HasIndex(r => r.FromPath).IsUnique();

        modelBuilder.Entity<NavItem>()
            .HasOne(i => i.Parent)
            .WithMany(i => i.Children)
            .HasForeignKey(i => i.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GuideFaq>()
            .HasOne(f => f.BlogPost)
            .WithMany()
            .HasForeignKey(f => f.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
