using HeartMeet.Data;
using HeartMeet.Data.Context;
using HeartMeet.Data.Repositories;
using HeartMeet.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Explicitly set WebRootPath
builder.WebHost.UseWebRoot("wwwroot");

var conn = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=heartmeet_db;Username=heartmeet;Password=heartmeet_pass";

builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseNpgsql(conn));
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(conn));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o =>
{
    o.Password.RequireDigit = false;
    o.Password.RequiredLength = 6;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath  = "/auth/login";
    o.LogoutPath = "/auth/logout";
    o.AccessDeniedPath = "/auth/login";
    o.Cookie.HttpOnly  = true;
    o.ExpireTimeSpan   = TimeSpan.FromDays(30);
    o.SlidingExpiration = true;
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<ILikeRepository, LikeRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<ILikeService, LikeService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 10 * 1024 * 1024);

var app = builder.Build();

// Seed DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DbSeeder.SeedAsync(db, um, rm);

    var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapControllers();
app.MapRazorComponents<HeartMeet.Web.Components.App>()
   .AddInteractiveServerRenderMode();

app.Run();
