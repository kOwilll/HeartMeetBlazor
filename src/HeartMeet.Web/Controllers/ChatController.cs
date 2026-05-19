using HeartMeet.Data.Repositories;
using HeartMeet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HeartMeet.Web.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
[IgnoreAntiforgeryToken]
public class ChatApiController(IChatRepository chatRepo, IChatService chatSvc, IProfileRepository profileRepo) : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest req)
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = (await profileRepo.GetByIdAsync(userId))?.FirstName ?? "Вы";
        var msg      = await chatSvc.SendAsync(req.ChatId, userId, userName, req.Text, req.RecipientId, req.MatchId);
        return Ok(new { id = msg.Id, text = msg.Text, createdAt = msg.CreatedAt.ToLocalTime().ToString("HH:mm"), mine = true });
    }

    [HttpGet("messages/{chatId:int}")]
    public async Task<IActionResult> GetMessages(int chatId)
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var messages = await chatRepo.GetMessagesAsync(chatId);
        await chatRepo.MarkReadAsync(chatId, userId);
        return Ok(messages.Select(m => new {
            id        = m.Id,
            text      = m.Text,
            mine      = m.SenderId == userId,
            createdAt = m.CreatedAt.ToLocalTime().ToString("HH:mm")
        }));
    }
}

public class SendMessageRequest
{
    public int    ChatId      { get; set; }
    public int    MatchId     { get; set; }
    public string Text        { get; set; } = "";
    public string RecipientId { get; set; } = "";
}
