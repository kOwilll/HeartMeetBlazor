using HeartMeet.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HeartMeet.Data.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Interest>        Interests        => Set<Interest>();
    public DbSet<ProfileInterest> ProfileInterests => Set<ProfileInterest>();
    public DbSet<Photo>           Photos           => Set<Photo>();
    public DbSet<Like>            Likes            => Set<Like>();
    public DbSet<Match>           Matches          => Set<Match>();
    public DbSet<Chat>            Chats            => Set<Chat>();
    public DbSet<Message>         Messages         => Set<Message>();
    public DbSet<Notification>    Notifications    => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<ProfileInterest>(e =>
        {
            e.HasKey(pi => new { pi.UserId, pi.InterestId });
            e.HasOne(pi => pi.Interest).WithMany(i => i.Profiles).HasForeignKey(pi => pi.InterestId);
        });

        b.Entity<Like>(e => { e.HasIndex(l => new { l.FromUserId, l.ToUserId }).IsUnique(); });

        b.Entity<Match>(e =>
        {
            e.HasOne(m => m.Chat).WithOne(c => c.Match).HasForeignKey<Chat>(c => c.MatchId);
        });
    }
}
