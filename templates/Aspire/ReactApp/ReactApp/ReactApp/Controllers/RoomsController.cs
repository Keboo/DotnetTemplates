using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReactApp.Core.QA;
using ReactApp.Core.Hubs;
using ReactApp.Data;

namespace ReactApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController(
    IRoomService roomService,
    IQuestionService questionService,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllRooms()
    {
        var rooms = await roomService.GetAllRoomsAsync();
        return Ok(rooms.Select(r => (RoomDto?)r));
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyRooms()
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var rooms = await roomService.GetRoomsByUserIdAsync(userId);
        return Ok(rooms.Select(r => (RoomDto?)r));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRoom(Guid id)
    {
        var room = await roomService.GetRoomByIdAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        return Ok((RoomDto?)room);
    }

    [HttpGet("name/{friendlyName}")]
    public async Task<IActionResult> GetRoomByName(string friendlyName)
    {
        var room = await roomService.GetRoomByFriendlyNameAsync(friendlyName);
        if (room == null)
        {
            return NotFound();
        }

        return Ok((RoomDto?)room);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var room = await roomService.CreateRoomAsync(request.FriendlyName, userId, cancellationToken);
        return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, (RoomDto?)room);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteRoom(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await roomService.DeleteRoomAsync(id, userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{roomId:guid}/questions")]
    public async Task<IActionResult> GetQuestions(Guid roomId)
    {
        var questions = await questionService.GetQuestionsByRoomIdAsync(roomId);
        return Ok(questions.Select(q => (QuestionDto)q));
    }

    [HttpGet("{roomId:guid}/questions/approved")]
    public async Task<IActionResult> GetApprovedQuestions(Guid roomId)
    {
        var questions = await questionService.GetApprovedQuestionsByRoomIdAsync(roomId);
        return Ok(questions.Select(q => (QuestionDto)q));
    }

    [HttpPost("{roomId:guid}/questions")]
    public async Task<IActionResult> CreateQuestion(Guid roomId, [FromBody] CreateQuestionRequest request, CancellationToken cancellationToken)
    {
        // Get client ID from header or generate one
        var clientId = Request.Headers["X-Client-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        // Check rate limit
        if (!await questionService.CanSubmitQuestionAsync(clientId))
        {
            return StatusCode(429, "Rate limit exceeded. Please wait before submitting another question.");
        }

        var question = await questionService.SubmitQuestionAsync(
            roomId,
            request.QuestionText,
            request.AuthorName,
            cancellationToken);

        return CreatedAtAction(nameof(GetQuestions), new { roomId }, (QuestionDto)question);
    }

    [HttpPut("{roomId:guid}/questions/{questionId:guid}/approve")]
    [Authorize]
    public async Task<IActionResult> ApproveQuestion(Guid roomId, Guid questionId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await questionService.ApproveQuestionAsync(questionId, userId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{roomId:guid}/questions/{questionId:guid}/answer")]
    [Authorize]
    public async Task<IActionResult> MarkQuestionAnswered(Guid roomId, Guid questionId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await questionService.MarkAsAnsweredAsync(questionId, userId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{roomId:guid}/questions/{questionId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteQuestion(Guid roomId, Guid questionId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await questionService.DeleteQuestionAsync(questionId, userId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{roomId:guid}/current-question/{questionId:guid?}")]
    [Authorize]
    public async Task<IActionResult> SetCurrentQuestion(Guid roomId, Guid? questionId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await roomService.SetCurrentQuestionAsync(roomId, questionId, userId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{roomId:guid}/current-question")]
    [Authorize]
    public async Task<IActionResult> ClearCurrentQuestion(Guid roomId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await roomService.SetCurrentQuestionAsync(roomId, null, userId, cancellationToken);
        return NoContent();
    }
}

public record CreateRoomRequest(string FriendlyName);
public record CreateQuestionRequest(string QuestionText, string AuthorName);
