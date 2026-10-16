using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

public record EnrolDto(int CategoryId);

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayContext _db;
    public EnrolmentsController(RaceDayContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Enrols the signed-in Participant in an event, under a chosen category.</summary>
    [HttpPost("events/{eventId}/enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enrol(int eventId, EnrolDto dto)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (!await _db.EventCategories.AnyAsync(ec => ec.EventId == eventId && ec.CategoryId == dto.CategoryId))
            return BadRequest("The chosen category is not linked to this event.");
        if (await _db.Enrolments.AnyAsync(en => en.EventId == eventId && en.UserId == CurrentUserId))
            return Conflict("You have already enrolled in this event.");
        var taken = await _db.Enrolments.CountAsync(en => en.EventId == eventId && en.Status != "Cancelled");
        if (taken >= ev.Capacity) return BadRequest("This event has no seats available.");

        var enrolment = new Enrolment { EventId = eventId, UserId = CurrentUserId, CategoryId = dto.CategoryId };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();
        return StatusCode(201, new { enrolment.Id, enrolment.EventId, enrolment.CategoryId, enrolment.Status });
    }

    /// <summary>Lists the signed-in Participant's own enrolments.</summary>
    [HttpGet("enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> MyEnrolments() =>
        Ok(await _db.Enrolments.Where(en => en.UserId == CurrentUserId)
            .Select(en => new { en.Id, en.EventId, EventName = en.Event.Name, en.CategoryId, en.Status, en.EnrolmentDate })
            .ToListAsync());

    /// <summary>Lists all enrolments for an event the Organiser owns.</summary>
    [HttpGet("events/{eventId}/enrolments")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> EventEnrolments(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();
        return Ok(await _db.Enrolments.Where(en => en.EventId == eventId)
            .Select(en => new { en.Id, en.UserId, en.User.FirstName, en.User.LastName, en.CategoryId, en.Status })
            .ToListAsync());
    }

    /// <summary>Cancels one of the signed-in Participant's enrolments.</summary>
    [HttpDelete("enrolment/{id}")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.FindAsync(id);
        if (enrolment == null) return NotFound();
        if (enrolment.UserId != CurrentUserId) return Forbid();
        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}