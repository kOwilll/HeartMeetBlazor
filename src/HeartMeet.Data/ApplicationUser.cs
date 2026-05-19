using HeartMeet.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HeartMeet.Data;

public class ApplicationUser : IdentityUser
{
    // Profile fields
    public string   FirstName   { get; set; } = "";
    public int      Age         { get; set; }
    public string   City        { get; set; } = "";
    public Gender   Gender      { get; set; }
    public LookingFor LookingFor { get; set; } = LookingFor.Any;
    public string   Bio         { get; set; } = "";
    public bool     IsBlocked   { get; set; }
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen    { get; set; } = DateTime.UtcNow;

    // Navigation — photos, interests via join table
}
