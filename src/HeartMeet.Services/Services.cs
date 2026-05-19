using HeartMeet.Data;
using HeartMeet.Data.Repositories;
using HeartMeet.Domain.Entities;
using HeartMeet.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HeartMeet.Services;

public interface ILikeService
{
    Task<bool> LikeAsync(string fromId, string toId);
    Task UnlikeAsync(string fromId, string toId);
    Task<bool> HasLikedAsync(string fromId, string toId);
}

public class LikeService(ILikeRepository likeRepo, IMatchRepository matchRepo, INotificationService notifSvc) : ILikeService
{
    public async Task<bool> LikeAsync(string fromId, string toId)
    {
        await likeRepo.LikeAsync(fromId, toId);
        if (await likeRepo.HasLikedAsync(toId, fromId))
        {
            var ex = await matchRepo.GetMatchAsync(fromId, toId);
            if (ex == null) { await matchRepo.CreateMatchAsync(fromId, toId); await notifSvc.SendMatchAsync(fromId, toId); }
            return true;
        }
        await notifSvc.SendLikeAsync(toId, fromId);
        return false;
    }
    public Task UnlikeAsync(string f, string t) => likeRepo.UnlikeAsync(f, t);
    public Task<bool> HasLikedAsync(string f, string t) => likeRepo.HasLikedAsync(f, t);
}

public interface INotificationService
{
    Task SendLikeAsync(string toUserId, string fromUserId);
    Task SendMatchAsync(string u1, string u2);
    Task SendMessageNotifAsync(string toUserId, string fromName, int matchId);
    event Action? OnChange;
}

public class NotificationService(INotificationRepository repo, UserManager<ApplicationUser> um) : INotificationService
{
    public event Action? OnChange;

    public async Task SendLikeAsync(string toUserId, string fromUserId)
    {
        var from = await um.FindByIdAsync(fromUserId);
        if (from == null) return;
        await repo.AddAsync(new Notification { UserId = toUserId, Title = $"{from.FirstName} оценил(а) вашу анкету",
            Body = "Посмотрите, кто вас лайкнул", Type = NotificationType.Like, LinkUrl = $"/profile/{fromUserId}" });
        OnChange?.Invoke();
    }

    public async Task SendMatchAsync(string u1, string u2)
    {
        var usr1 = await um.FindByIdAsync(u1); var usr2 = await um.FindByIdAsync(u2);
        if (usr1 == null || usr2 == null) return;
        await repo.AddAsync(new Notification { UserId = u1, Title = $"Взаимная симпатия с {usr2.FirstName}! 🎉",
            Body = "Теперь вы можете общаться", Type = NotificationType.Match, LinkUrl = "/chats" });
        await repo.AddAsync(new Notification { UserId = u2, Title = $"Взаимная симпатия с {usr1.FirstName}! 🎉",
            Body = "Теперь вы можете общаться", Type = NotificationType.Match, LinkUrl = "/chats" });
        OnChange?.Invoke();
    }

    public async Task SendMessageNotifAsync(string toUserId, string fromName, int matchId)
    {
        await repo.AddAsync(new Notification { UserId = toUserId, Title = $"Сообщение от {fromName}",
            Type = NotificationType.Message, LinkUrl = $"/chats/{matchId}" });
        OnChange?.Invoke();
    }
}

public interface IChatService
{
    Task<Message> SendAsync(int chatId, string senderId, string senderName, string text, string recipientId, int matchId);
}

public class ChatService(IChatRepository repo, INotificationService notifSvc) : IChatService
{
    public async Task<Message> SendAsync(int chatId, string senderId, string senderName, string text, string recipientId, int matchId)
    {
        var msg = await repo.SendMessageAsync(chatId, senderId, text);
        await notifSvc.SendMessageNotifAsync(recipientId, senderName, matchId);
        return msg;
    }
}
