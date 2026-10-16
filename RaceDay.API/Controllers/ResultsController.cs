using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

public record ResultDto(TimeSpan? FinishTime, int? Position);

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayContext _db;
    public ResultsController(RaceDayContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Records a finish time and position for an enrolment. Organisers only.</summary>
    [HttpPost("enrolments/{enrolmentId}/result")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(int enrolmentId, ResultDto dto)
    {
        var enrolment = await _db.Enrolments.Include(en => en.Event).FirstOrDefaultAsync(en => en.Id == enrolmentId);
        if (enrolment == null) return NotFound();
        if (enrolment.Event.OrganiserId != CurrentUserId) return Forbid();
        if (dto.Position is <= 0) return BadRequest("Result information is invalid.");
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
            return Conflict("A result already exists for this enrolment.");

        var result = new Result { EnrolmentId = enrolmentId, FinishTime = dto.FinishTime, Position = dto.Position };
        enrolment.Status = "Completed";
        _db.Results.Add(result);
        await _db.SaveChangesAsync();
        return StatusCode(201, new { result.Id, result.EnrolmentId, result.FinishTime, result.Position });
    }

    /// <summary>Updates a result for an event the Organiser owns.</summary>
    [HttpPut("results/{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, ResultDto dto)
    {
        var result = await _db.Results.Include(r => r.Enrolment).ThenInclude(en => en.Event)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (result == null) return NotFound();
        if (result.Enrolment.Event.OrganiserId != CurrentUserId) return Forbid();
        if (dto.Position is <= 0) return BadRequest("Result information is invalid.");

        result.FinishTime = dto.FinishTime;
        result.Position = dto.Position;
        await _db.SaveChangesAsync();
        return Ok(new { result.Id, result.EnrolmentId, result.FinishTime, result.Position });
    }

    /// <summary>Lists the recorded results for an event. Public.</summary>
    [HttpGet("events/{eventId}/results")]
    public async Task<IActionResult> EventResults(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();
        return Ok(await _db.Results.Where(r => r.Enrolment.EventId == eventId)
            .Select(r => new { r.Id, r.Enrolment.User.FirstName, r.Enrolment.User.LastName, r.FinishTime, r.Position })
            .ToListAsync());
    }

    /// <summary>Lists the signed-in Participant's own results across all events.</summary>
    [HttpGet("results/me")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> MyResults() =>
        Ok(await _db.Results.Where(r => r.Enrolment.UserId == CurrentUserId)
            .Select(r => new { r.Id, EventName = r.Enrolment.Event.Name, r.Enrolment.Event.EventDate, r.FinishTime, r.Position })
            .ToListAsync());
}
