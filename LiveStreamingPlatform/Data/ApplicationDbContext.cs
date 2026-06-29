using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LiveStreamingPlatform.Models;

namespace LiveStreamingPlatform.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Channel> Channels { get; set; }

        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<Follow> Follows { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Follow>()
                .HasIndex(f => new { f.FollowerId, f.ChannelId })
                .IsUnique();
        }
    }
}
