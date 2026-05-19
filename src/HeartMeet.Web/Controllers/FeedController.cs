using HeartMeet.Data.Repositories;
using HeartMeet.Domain.Enums;
using HeartMeet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HeartMeet.Web.Controllers;

[ApiController]
[Route("api/feed")]
[Authorize]
[Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
public class FeedController(ILikeService likeService, IProfileRepository profileRepo) : ControllerBase
{
    [HttpPost("like/{targetId}")]
    public async Task<IActionResult> Like(string targetId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isMatch = await likeService.LikeAsync(userId, targetId);
        return Ok(new { isMatch });
    }

    [HttpPost("nope/{targetId}")]
    public async Task<IActionResult> Nope(string targetId)
    {
        return Ok(new { skipped = true });
    }

    [HttpGet("queue")]
    public async Task<IActionResult> Queue()
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var me      = await profileRepo.GetByIdAsync(userId);

        // Determine which gender to show based on current user's gender
        Gender? genderFilter = null;
        if (me != null)
        {
            genderFilter = me.Gender switch
            {
                Gender.Male   => Gender.Female,
                Gender.Female => Gender.Male,
                _             => null  // Other: show everyone
            };
        }

        var users  = await profileRepo.GetFeedAsync(userId, genderFilter, null, null, null, new(), 0, 50);
        var result = new List<object>();
        foreach (var u in users)
        {
            var photos = await profileRepo.GetPhotosAsync(u.Id);
            var ids    = await profileRepo.GetUserInterestIdsAsync(u.Id);
            var allI   = await profileRepo.GetInterestsAsync();
            result.Add(new {
                id        = u.Id,
                name      = u.FirstName,
                age       = u.Age,
                city      = u.City,
                bio       = u.Bio,
                photos    = photos.Select(p => p.Url).ToList(),
                interests = allI.Where(i => ids.Contains(i.Id)).Select(i => i.Name).ToList()
            });
        }
        return Ok(result);
    }
}


// ProfileController-like endpoints
