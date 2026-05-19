using HeartMeet.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HeartMeet.Web.Controllers;

[Route("account")]
public class AccountController(UserManager<ApplicationUser> um, SignInManager<ApplicationUser> sm) : Controller
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl)
    {
        var user = await um.FindByEmailAsync(email);
        if (user == null) return Redirect("/auth/login?error=invalid");
        if (user.IsBlocked) return Redirect("/auth/login?error=blocked");
        var r = await sm.PasswordSignInAsync(user, password, true, false);
        if (!r.Succeeded) return Redirect("/auth/login?error=invalid");
        return Redirect(returnUrl ?? "/feed");
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(string email, string password, string firstName, int age, string? city)
    {
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FirstName = firstName, Age = age, City = city ?? "" };
        var r = await um.CreateAsync(user, password);
        if (!r.Succeeded) return Redirect($"/auth/register?error={Uri.EscapeDataString(string.Join("; ", r.Errors.Select(e => e.Description)))}");
        await um.AddToRoleAsync(user, "User");
        await sm.SignInAsync(user, true);
        return Redirect("/profile/edit?welcome=1");
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout() { await sm.SignOutAsync(); return Redirect("/auth/login"); }
}
