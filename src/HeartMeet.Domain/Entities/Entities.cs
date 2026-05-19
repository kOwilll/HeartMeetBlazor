using HeartMeet.Domain.Enums;

namespace HeartMeet.Domain.Entities;

// ── Interest tag ──────────────────────────────────────────────────────────
public class Interest
{
    public int    Id   { get; set; }
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "heart";
    public ICollection<ProfileInterest> Profiles { get; set; } = new List<ProfileInterest>();
}

public class ProfileInterest
{
    public string    UserId   { get; set; } = "";
    public int       InterestId { get; set; }
    public Interest  Interest { get; set; } = null!;
}

// ── Photo ─────────────────────────────────────────────────────────────────
public class Photo
{
    public int    Id       { get; set; }
    public string UserId   { get; set; } = "";
    public string Url      { get; set; } = "";
    public bool   IsAvatar { get; set; }
    public int    Order    { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── Like ──────────────────────────────────────────────────────────────────
public class Like
{
    public int    Id         { get; set; }
    public string FromUserId { get; set; } = "";
    public string ToUserId   { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── Match (mutual like) ───────────────────────────────────────────────────
public class Match
{
    public int    Id      { get; set; }
    public string User1Id { get; set; } = "";
    public string User2Id { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Chat? Chat { get; set; }
}

// ── Chat ──────────────────────────────────────────────────────────────────
public class Chat
{
    public int    Id      { get; set; }
    public int    MatchId { get; set; }
    public Match  Match   { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── Message ───────────────────────────────────────────────────────────────
public class Message
{
    public int    Id        { get; set; }
    public int    ChatId    { get; set; }
    public Chat   Chat      { get; set; } = null!;
    public string SenderId  { get; set; } = "";
    public string Text      { get; set; } = "";
    public bool   IsRead    { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── Notification ──────────────────────────────────────────────────────────
public class Notification
{
    public int    Id        { get; set; }
    public string UserId    { get; set; } = "";
    public string Title     { get; set; } = "";
    public string Body      { get; set; } = "";
    public string? LinkUrl  { get; set; }
    public NotificationType Type { get; set; }
    public bool   IsRead    { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
