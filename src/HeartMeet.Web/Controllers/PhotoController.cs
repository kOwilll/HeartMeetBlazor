using HeartMeet.Data;
using HeartMeet.Data.Repositories;
using HeartMeet.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HeartMeet.Web.Controllers;

[ApiController]
[Route("api/photo")]
[Authorize]
[IgnoreAntiforgeryToken]
public class PhotoController(IProfileRepository profileRepo, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Файл не выбран" });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { error = "Файл слишком большой (макс. 10 МБ)" });

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(ext))
            return BadRequest(new { error = "Формат не поддерживается" });

        var photos = await profileRepo.GetPhotosAsync(userId);
        if (photos.Count >= 4)
            return BadRequest(new { error = "Максимум 4 фотографии" });

        var webRoot = string.IsNullOrEmpty(env.WebRootPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
            : env.WebRootPath;
        var dir  = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(dir);

        var name     = Guid.NewGuid().ToString("N") + ext;
        var fullPath = Path.Combine(dir, name);
        await using var fs = System.IO.File.Create(fullPath);
        await file.CopyToAsync(fs);

        var url = "/uploads/" + name;
        await profileRepo.AddPhotoAsync(new Photo { UserId = userId, Url = url });

        return Ok(new { url, success = true });
    }

    [HttpDelete("{photoId:int}")]
    public async Task<IActionResult> Delete(int photoId)
    {
        await profileRepo.DeletePhotoAsync(photoId);
        return Ok(new { success = true });
    }
}
