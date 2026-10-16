using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

public record EventDto(string Name, string Description, DateOnly EventDate,
    string Location, decimal DistanceKm, int Capacity, string EventType);

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayContext _db;
    public EventsController(RaceDayContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lists all events. Public.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Events.Select(e => new { e.Id, e.Name, e.EventDate, e.Location, e.DistanceKm, e.EventType }).ToListAsync());

    /// <summary>Gets one event by ID. Public.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        return ev == null ? NotFound() : Ok(ev);
    }

    /// <summary>Creates an event owned by the signed-in Organiser.</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(EventDto dto)
    {
        if (!new[] { "Run", "Walk", "Cycle" }.Contains(dto.EventType) || dto.Capacity <= 0 || dto.DistanceKm <= 0)
            return BadRequest("Invalid event details.");

        var ev = new Event
        {
            OrganiserId = CurrentUserId,
            Name = dto.Name,
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location,
            DistanceKm = dto.DistanceKm,
            Capacity = dto.Capacity,
            EventType = dto.EventType
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return StatusCode(201, ev);
    }

    /// <summary>Updates an event. Only the Organiser who owns it may update it.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        ev.Name = dto.Name; ev.Description = dto.Description; ev.EventDate = dto.EventDate;
        ev.Location = dto.Location; ev.DistanceKm = dto.DistanceKm; ev.Capacity = dto.Capacity;
        ev.EventType = dto.EventType;
        await _db.SaveChangesAsync();
        return Ok(ev);
    }

    /// <summary>Deletes an event owned by the signed-in Organiser. Returns 409 if enrolments exist.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();
        if (await _db.Enrolments.AnyAsync(en => en.EventId == id))
            return Conflict("This event has enrolments and cannot be deleted.");

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}