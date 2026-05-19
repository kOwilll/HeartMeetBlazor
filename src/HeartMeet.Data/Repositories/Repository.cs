using HeartMeet.Data.Context;
using HeartMeet.Domain.Entities;
using HeartMeet.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HeartMeet.Data.Repositories;

// ── Profile / Feed ────────────────────────────────────────────────────────
public interface IProfileRepository
{
    Task<List<ApplicationUser>> GetFeedAsync(string currentUserId, Gender? gender, int? ageFrom, int? ageTo, string? city, List<int> interestIds, int skip, int take);
    Task<ApplicationUser?> GetByIdAsync(string id);
    Task<List<Photo>> GetPhotosAsync(string userId);
    Task<Photo?> GetAvatarAsync(string userId);
    Task AddPhotoAsync(Photo photo);
    Task DeletePhotoAsync(int photoId);
    Task<List<Interest>> GetInterestsAsync();
    Task<List<int>> GetUserInterestIdsAsync(string userId);
    Task SetInterestsAsync(string userId, List<int> interestIds);
    Task UpdateLastSeenAsync(string userId);
}

public class ProfileRepository(IDbContextFactory<AppDbContext> factory) : IProfileRepository
{
    public async Task<List<ApplicationUser>> GetFeedAsync(string currentUserId, Gender? gender, int? ageFrom, int? ageTo, string? city, List<int> interestIds, int skip, int take)
    {
        await using var db = await factory.CreateDbContextAsync();
        // Exclude self, blocked, already liked
        var likedIds = await db.Likes.Where(l => l.FromUserId == currentUserId).Select(l => l.ToUserId).ToListAsync();
        likedIds.Add(currentUserId);

        var q = db.Users.Where(u => !likedIds.Contains(u.Id) && !u.IsBlocked);
        if (gender.HasValue) q = q.Where(u => u.Gender == gender.Value);
        if (ageFrom.HasValue) q = q.Where(u => u.Age >= ageFrom.Value);
        if (ageTo.HasValue)   q = q.Where(u => u.Age <= ageTo.Value);
        if (!string.IsNullOrEmpty(city)) q = q.Where(u => u.City.ToLower().Contains(city.ToLower()));
        if (interestIds.Any())
            q = q.Where(u => db.ProfileInterests.Any(pi => pi.UserId == u.Id && interestIds.Contains(pi.InterestId)));

        return await q.OrderByDescending(u => u.LastSeen).Skip(skip).Take(take).ToListAsync();
    }

    public async Task<ApplicationUser?> GetByIdAsync(string id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<List<Photo>> GetPhotosAsync(string userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Photos.Where(p => p.UserId == userId).OrderBy(p => p.Order).ToListAsync();
    }

    public async Task<Photo?> GetAvatarAsync(string userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Photos.Where(p => p.UserId == userId).OrderByDescending(p => p.IsAvatar).ThenBy(p => p.Order).FirstOrDefaultAsync();
    }

    public async Task AddPhotoAsync(Photo photo)
    {
        await using var db = await factory.CreateDbContextAsync();
        // Max 4 photos (1 avatar + 3 extra)
        var count = await db.Photos.CountAsync(p => p.UserId == photo.UserId);
        if (count >= 4) return;
        if (!await db.Photos.AnyAsync(p => p.UserId == photo.UserId)) photo.IsAvatar = true;
        photo.Order = count;
        db.Photos.Add(photo);
        await db.SaveChangesAsync();
    }

    public async Task DeletePhotoAsync(int photoId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var p = await db.Photos.FindAsync(photoId);
        if (p == null) return;
        var wasAvatar = p.IsAvatar;
        db.Photos.Remove(p);
        await db.SaveChangesAsync();
        if (wasAvatar)
        {
            var next = await db.Photos.Where(x => x.UserId == p.UserId).OrderBy(x => x.Order).FirstOrDefaultAsync();
            if (next != null) { next.IsAvatar = true; await db.SaveChangesAsync(); }
        }
    }

    public async Task<List<Interest>> GetInterestsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Interests.OrderBy(i => i.Name).ToListAsync();
    }

    public async Task<List<int>> GetUserInterestIdsAsync(string userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.ProfileInterests.Where(pi => pi.UserId == userId).Select(pi => pi.InterestId).ToListAsync();
    }

    public async Task SetInterestsAsync(string userId, List<int> interestIds)
    {
        await using var db = await factory.CreateDbContextAsync();
        var existing = db.ProfileInterests.Where(pi => pi.UserId == userId);
        db.ProfileInterests.RemoveRange(existing);
        foreach (var id in interestIds.Distinct())
            db.ProfileInterests.Add(new ProfileInterest { UserId = userId, InterestId = id });
        await db.SaveChangesAsync();
    }

    public async Task UpdateLastSeenAsync(string userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var u = await db.Users.FindAsync(userId);
        if (u != null) { u.LastSeen = DateTime.UtcNow; await db.SaveChangesAsync(); }
    }
}

// ── Like ──────────────────────────────────────────────────────────────────
public interface ILikeRepository
{
    Task<bool> HasLikedAsync(string fromId, string toId);
    Task LikeAsync(string fromId, string toId);
    Task UnlikeAsync(string fromId, string toId);
    Task<List<string>> GetLikerIdsAsync(string userId);
    Task<int> GetLikeCountAsync(string userId);
}

public class LikeRepository(IDbContextFactory<AppDbContext> factory) : ILikeRepository
{
    public async Task<bool> HasLikedAsync(string fromId, string toId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Likes.AnyAsync(l => l.FromUserId == fromId && l.ToUserId == toId);
    }

    public async Task LikeAsync(string fromId, string toId)
    {
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Likes.AnyAsync(l => l.FromUserId == fromId && l.ToUserId == toId))
        {
            db.Likes.Add(new Like { FromUserId = fromId, ToUserId = toId });
            await db.SaveChangesAsync();
        }
    }

    public async Task UnlikeAsync(string fromId, string toId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var like = await db.Likes.FirstOrDefaultAsync(l => l.FromUserId == fromId && l.ToUserId == toId);
        if (like != null) { db.Likes.Remove(like); await db.SaveChangesAsync(); }
    }

    public async Task<List<string>> GetLikerIdsAsync(string userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Likes.Where(l => l.ToUserId == userId).Select(l => l.FromUserId).ToListAsync();
    }

    public async Task<int> GetLikeCountAsync(string userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Likes.CountAsync(l => l.ToUserId == userId);
    }
}

// ── Match ─────────────────────────────────────────────────────────────────
public interface IMatchRepository
{
    Task<Match?> GetMatchAsync(string u1, string u2);
    Task<Match> CreateMatchAsync(string u1, string u2);
    Task<List<Match>> GetUserMatchesAsync(string userId);
    Task<Match?> GetMatchWithChatAsync(int matchId);
}

public class MatchRepository(IDbContextFactory<AppDbContext> factory) : IMatchRepository
{
    public async Task<Match?> GetMatchAsync(string u1, string u2)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Matches.Include(m => m.Chat)
            .FirstOrDefaultAsync(m => (m.User1Id == u1 && m.User2Id == u2) || (m.User1Id == u2 && m.User2Id == u1));
    }

    public async Task<Match> CreateMatchAsync(string u1, string u2)
    {
        await using var db = await factory.CreateDbContextAsync();
        var match = new Match { User1Id = u1, User2Id = u2 };
        db.Matches.Add(match);
        await db.SaveChangesAsync();
        var chat = new Chat { MatchId = match.Id };
        db.Chats.Add(chat);
        await db.SaveChangesAsync();
        return match;
    }

    public async Task<List<Match>> GetUserMatchesAsync(string userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Matches.Include(m => m.Chat).ThenInclude(c => c!.Messages.OrderByDescending(msg => msg.CreatedAt).Take(1))
            .Where(m => m.User1Id == userId || m.User2Id == userId)
            .OrderByDescending(m => m.CreatedAt).ToListAsync();
    }

    public async Task<Match?> GetMatchWithChatAsync(int matchId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Matches.Include(m => m.Chat).ThenInclude(c => c!.Messages.OrderBy(msg => msg.CreatedAt))
            .FirstOrDefaultAsync(m => m.Id == matchId);
    }
}

// ── Chat / Message ────────────────────────────────────────────────────────
public interface IChatRepository
{
    Task<Chat?> GetChatAsync(int chatId);
    Task<List<Message>> GetMessagesAsync(int chatId);
    Task<Message> SendMessageAsync(int chatId, string senderId, string text);
    Task MarkReadAsync(int chatId, string userId);
    Task<int> GetUnreadCountAsync(string userId);
}

public class ChatRepository(IDbContextFactory<AppDbContext> factory) : IChatRepository
{
    public async Task<Chat?> GetChatAsync(int chatId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Chats.Include(c => c.Match).FirstOrDefaultAsync(c => c.Id == chatId);
    }

    public async Task<List<Message>> GetMessagesAsync(int chatId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Messages.Where(m => m.ChatId == chatId).OrderBy(m => m.CreatedAt).ToListAsync();
    }

    public async Task<Message> SendMessageAsync(int chatId, string senderId, string text)
    {
        await using var db = await factory.CreateDbContextAsync();
        var msg = new Message { ChatId = chatId, SenderId = senderId, Text = text };
        db.Messages.Add(msg);
        await db.SaveChangesAsync();
        return msg;
    }

    public async Task MarkReadAsync(int chatId, string userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        await db.Messages.Where(m => m.ChatId == chatId && m.SenderId != userId && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var userMatchIds = await db.Matches.Where(m => m.User1Id == userId || m.User2Id == userId).Select(m => m.Id).ToListAsync();
        var chatIds = await db.Chats.Where(c => userMatchIds.Contains(c.MatchId)).Select(c => c.Id).ToListAsync();
        return await db.Messages.CountAsync(m => chatIds.Contains(m.ChatId) && m.SenderId != userId && !m.IsRead);
    }
}

// ── Notification ──────────────────────────────────────────────────────────
public interface INotificationRepository
{
    Task AddAsync(Notification n);
    Task<List<Notification>> GetForUserAsync(string userId, int count = 20);
    Task MarkReadAsync(int id);
    Task MarkAllReadAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
}

public class NotificationRepository(IDbContextFactory<AppDbContext> factory) : INotificationRepository
{
    public async Task AddAsync(Notification n)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Notifications.Add(n);
        await db.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetForUserAsync(string userId, int count = 20)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).Take(count).ToListAsync();
    }

    public async Task MarkReadAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        await db.Notifications.Where(n => n.Id == id).ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task MarkAllReadAsync(string userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        await db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }
}

// ── Admin ─────────────────────────────────────────────────────────────────
public interface IAdminRepository
{
    Task<List<ApplicationUser>> GetAllUsersAsync();
    Task BlockAsync(string userId, bool block);
    Task DeleteUserAsync(string userId);
}

public class AdminRepository(IDbContextFactory<AppDbContext> factory) : IAdminRepository
{
    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
    }

    public async Task BlockAsync(string userId, bool block)
    {
        await using var db = await factory.CreateDbContextAsync();
        var u = await db.Users.FindAsync(userId);
        if (u != null) { u.IsBlocked = block; await db.SaveChangesAsync(); }
    }

    public async Task DeleteUserAsync(string userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var u = await db.Users.FindAsync(userId);
        if (u != null)
        {
            db.Photos.RemoveRange(db.Photos.Where(p => p.UserId == userId));
            db.Likes.RemoveRange(db.Likes.Where(l => l.FromUserId == userId || l.ToUserId == userId));
            db.Users.Remove(u);
            await db.SaveChangesAsync();
        }
    }
}
