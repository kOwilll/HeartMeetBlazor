using HeartMeet.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HeartMeet.Web.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[IgnoreAntiforgeryToken]
public class AdminApiController(IAdminRepository adminRepo) : ControllerBase
{
    [HttpPost("block/{userId}")]
    public async Task<IActionResult> Block(string userId)
    {
        if (userId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            return BadRequest(new { error = "Нельзя заблокировать себя" });
        await adminRepo.BlockAsync(userId, true);
        return Ok(new { success = true, blocked = true });
    }

    [HttpPost("unblock/{userId}")]
    public async Task<IActionResult> Unblock(string userId)
    {
        if (userId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            return BadRequest(new { error = "Нельзя разблокировать себя" });
        await adminRepo.BlockAsync(userId, false);
        return Ok(new { success = true, blocked = false });
    }

    [HttpDelete("user/{userId}")]
    public async Task<IActionResult> Delete(string userId)
    {
        if (userId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            return BadRequest(new { error = "Нельзя удалить себя" });
        await adminRepo.DeleteUserAsync(userId);
        return Ok(new { success = true });
    }
}
