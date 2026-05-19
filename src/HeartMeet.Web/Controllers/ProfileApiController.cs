using HeartMeet.Data;
using HeartMeet.Data.Context;
using HeartMeet.Data.Repositories;
using HeartMeet.Domain.Enums;
using HeartMeet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HeartMeet.Web.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
[IgnoreAntiforgeryToken]
public class ProfileApiController(
    ILikeService likeService,
    IProfileRepository profileRepo,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpPost("like/{targetId}")]
    public async Task<IActionResult> Like(string targetId)
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isMatch = await likeService.LikeAsync(userId, targetId);
        return Ok(new { isMatch, liked = true });
    }

    [HttpPost("unlike/{targetId}")]
    public async Task<IActionResult> Unlike(string targetId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await likeService.UnlikeAsync(userId, targetId);
        return Ok(new { liked = false });
    }

    [HttpGet("liked/{targetId}")]
    public async Task<IActionResult> IsLiked(string targetId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var liked  = await likeService.HasLikedAsync(userId, targetId);
        return Ok(new { liked });
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] SaveProfileRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user   = await userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        user.FirstName  = (req.FirstName ?? "").Trim();
        user.Age        = req.Age;
        user.City       = (req.City ?? "").Trim();
        user.Bio        = (req.Bio  ?? "").Trim();
        user.Gender     = Enum.Parse<Gender>(req.Gender ?? "Male");
        user.LookingFor = Enum.Parse<LookingFor>(req.LookingFor ?? "Any");

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { error = string.Join(", ", result.Errors.Select(e => e.Description)) });

        if (req.Interests != null)
            await profileRepo.SetInterestsAsync(userId, req.Interests);

        return Ok(new { success = true });
    }
}

public class SaveProfileRequest
{
    public string? FirstName  { get; set; }
    public int     Age        { get; set; }
    public string? City       { get; set; }
    public string? Bio        { get; set; }
    public string? Gender     { get; set; }
    public string? LookingFor { get; set; }
    public List<int>? Interests { get; set; }
}
