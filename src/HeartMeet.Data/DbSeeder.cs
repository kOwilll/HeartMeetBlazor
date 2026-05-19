using HeartMeet.Data.Context;
using HeartMeet.Domain.Entities;
using HeartMeet.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HeartMeet.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, UserManager<ApplicationUser> um, RoleManager<IdentityRole> rm)
    {
        // Schema
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""AspNetRoles"" (""Id"" text PRIMARY KEY, ""Name"" text, ""NormalizedName"" text, ""ConcurrencyStamp"" text);
            CREATE TABLE IF NOT EXISTS ""AspNetUsers"" (
                ""Id"" text PRIMARY KEY, ""FirstName"" text NOT NULL DEFAULT '',
                ""Age"" integer NOT NULL DEFAULT 0, ""City"" text NOT NULL DEFAULT '',
                ""Gender"" integer NOT NULL DEFAULT 0, ""LookingFor"" integer NOT NULL DEFAULT 2,
                ""Bio"" text NOT NULL DEFAULT '', ""IsBlocked"" boolean NOT NULL DEFAULT false,
                ""CreatedAt"" timestamptz NOT NULL DEFAULT now(),
                ""LastSeen"" timestamptz NOT NULL DEFAULT now(),
                ""UserName"" text, ""NormalizedUserName"" text, ""Email"" text,
                ""NormalizedEmail"" text, ""EmailConfirmed"" boolean NOT NULL DEFAULT true,
                ""PasswordHash"" text, ""SecurityStamp"" text, ""ConcurrencyStamp"" text,
                ""PhoneNumber"" text, ""PhoneNumberConfirmed"" boolean NOT NULL DEFAULT false,
                ""TwoFactorEnabled"" boolean NOT NULL DEFAULT false,
                ""LockoutEnd"" timestamptz, ""LockoutEnabled"" boolean NOT NULL DEFAULT false,
                ""AccessFailedCount"" integer NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS ""AspNetUserRoles"" (""UserId"" text, ""RoleId"" text, PRIMARY KEY (""UserId"",""RoleId""));
            CREATE TABLE IF NOT EXISTS ""AspNetUserClaims"" (""Id"" serial PRIMARY KEY,""UserId"" text,""ClaimType"" text,""ClaimValue"" text);
            CREATE TABLE IF NOT EXISTS ""AspNetUserLogins"" (""LoginProvider"" text,""ProviderKey"" text,""ProviderDisplayName"" text,""UserId"" text, PRIMARY KEY(""LoginProvider"",""ProviderKey""));
            CREATE TABLE IF NOT EXISTS ""AspNetUserTokens"" (""UserId"" text,""LoginProvider"" text,""Name"" text,""Value"" text, PRIMARY KEY(""UserId"",""LoginProvider"",""Name""));
            CREATE TABLE IF NOT EXISTS ""AspNetRoleClaims"" (""Id"" serial PRIMARY KEY,""RoleId"" text,""ClaimType"" text,""ClaimValue"" text);
            CREATE TABLE IF NOT EXISTS ""Interests"" (""Id"" serial PRIMARY KEY, ""Name"" text NOT NULL, ""Icon"" text NOT NULL DEFAULT 'heart');
            CREATE TABLE IF NOT EXISTS ""ProfileInterests"" (""UserId"" text NOT NULL, ""InterestId"" integer NOT NULL, PRIMARY KEY(""UserId"",""InterestId""));
            CREATE TABLE IF NOT EXISTS ""Photos"" (""Id"" serial PRIMARY KEY, ""UserId"" text NOT NULL, ""Url"" text NOT NULL, ""IsAvatar"" boolean NOT NULL DEFAULT false, ""Order"" integer NOT NULL DEFAULT 0, ""CreatedAt"" timestamptz NOT NULL DEFAULT now());
            CREATE TABLE IF NOT EXISTS ""Likes"" (""Id"" serial PRIMARY KEY, ""FromUserId"" text NOT NULL, ""ToUserId"" text NOT NULL, ""CreatedAt"" timestamptz NOT NULL DEFAULT now());
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Likes_From_To"" ON ""Likes""(""FromUserId"",""ToUserId"");
            CREATE TABLE IF NOT EXISTS ""Matches"" (""Id"" serial PRIMARY KEY, ""User1Id"" text NOT NULL, ""User2Id"" text NOT NULL, ""CreatedAt"" timestamptz NOT NULL DEFAULT now());
            CREATE TABLE IF NOT EXISTS ""Chats"" (""Id"" serial PRIMARY KEY, ""MatchId"" integer NOT NULL UNIQUE REFERENCES ""Matches""(""Id"") ON DELETE CASCADE, ""CreatedAt"" timestamptz NOT NULL DEFAULT now());
            CREATE TABLE IF NOT EXISTS ""Messages"" (""Id"" serial PRIMARY KEY, ""ChatId"" integer NOT NULL REFERENCES ""Chats""(""Id"") ON DELETE CASCADE, ""SenderId"" text NOT NULL, ""Text"" text NOT NULL, ""IsRead"" boolean NOT NULL DEFAULT false, ""CreatedAt"" timestamptz NOT NULL DEFAULT now());
            CREATE TABLE IF NOT EXISTS ""Notifications"" (""Id"" serial PRIMARY KEY, ""UserId"" text NOT NULL, ""Title"" text NOT NULL, ""Body"" text NOT NULL DEFAULT '', ""LinkUrl"" text, ""Type"" integer NOT NULL DEFAULT 0, ""IsRead"" boolean NOT NULL DEFAULT false, ""CreatedAt"" timestamptz NOT NULL DEFAULT now());
        ");

        // Roles
        foreach (var role in new[] { "Admin", "User" })
            if (!await db.Roles.AnyAsync(r => r.Name == role))
                await rm.CreateAsync(new IdentityRole(role));

        // Interests
        if (!await db.Interests.AnyAsync())
        {
            db.Interests.AddRange(
                new Interest { Name = "Музыка",        Icon = "music" },
                new Interest { Name = "Спорт",         Icon = "activity" },
                new Interest { Name = "Путешествия",   Icon = "map-pin" },
                new Interest { Name = "Кино",          Icon = "film" },
                new Interest { Name = "Книги",         Icon = "book-open" },
                new Interest { Name = "Фотография",    Icon = "camera" },
                new Interest { Name = "Кулинария",     Icon = "coffee" },
                new Interest { Name = "Искусство",     Icon = "palette" },
                new Interest { Name = "Технологии",    Icon = "cpu" },
                new Interest { Name = "Природа",       Icon = "leaf" },
                new Interest { Name = "Танцы",         Icon = "music-2" },
                new Interest { Name = "Йога",          Icon = "heart" }
            );
            await db.SaveChangesAsync();
        }

        // Admin user
        await EnsureUser(um, "admin@heartmeet.ru", "Admin123!", "Admin", "Администратор", 30, Gender.Male);

        // Test users
        await EnsureUser(um, "anna@test.ru",   "Test123!", "User", "Анна",   25, Gender.Female, "Москва",    "Люблю кофе, путешествия и хорошие книги");
        await EnsureUser(um, "kate@test.ru",   "Test123!", "User", "Катя",   23, Gender.Female, "СПб",       "Фотограф по призванию, путешественник по жизни");
        await EnsureUser(um, "alex@test.ru",   "Test123!", "User", "Алексей", 28, Gender.Male, "Москва",    "Занимаюсь спортом, люблю готовить");
        await EnsureUser(um, "masha@test.ru",  "Test123!", "User", "Маша",   26, Gender.Female, "Казань",    "Художник, люблю всё красивое");
        await EnsureUser(um, "dmitry@test.ru", "Test123!", "User", "Дмитрий", 31, Gender.Male, "Москва",    "Технарь с душой романтика");
    }

    static async Task EnsureUser(UserManager<ApplicationUser> um, string email, string password,
        string role, string name, int age, Gender gender, string city = "", string bio = "")
    {
        var existing = await um.FindByEmailAsync(email);
        if (existing != null)
        {
            // Always update bio to remove old emojis
            existing.Bio = bio;
            await um.UpdateAsync(existing);
            return;
        }
        var user = new ApplicationUser
        {
            UserName = email, Email = email, EmailConfirmed = true,
            FirstName = name, Age = age, Gender = gender,
            City = city, Bio = bio,
        };
        var result = await um.CreateAsync(user, password);
        if (result.Succeeded) await um.AddToRoleAsync(user, role);
    }
}
