using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

public record CategoryDto(string Name, string? Description);

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayContext _db;
    public CategoriesController(RaceDayContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lists all categories. Public.</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Categories.Select(c => new { c.Id, c.Name, c.Description }).ToListAsync());

    /// <summary>Lists the categories linked to an event. Public.</summary>
    [HttpGet("events/{eventId}/categories")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();
        return Ok(await _db.EventCategories.Where(ec => ec.EventId == eventId)
            .Select(ec => new { ec.Category.Id, ec.Category.Name, ec.Category.Description })
            .ToListAsync());
    }

    /// <summary>Creates a category. Organisers only.</summary>
    [HttpPost("categories")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(CategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Category information is invalid.");
        if (await _db.Categories.AnyAsync(c => c.Name == dto.Name))
            return Conflict("A category with the same name already exists.");
        var cat = new Category { Name = dto.Name, Description = dto.Description };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return StatusCode(201, new { cat.Id, cat.Name, cat.Description });
    }

    /// <summary>Links an existing category to an event the Organiser owns.</summary>
    [HttpPost("events/{eventId}/categories/{categoryId}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Link(int eventId, int categoryId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null || !await _db.Categories.AnyAsync(c => c.Id == categoryId)) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();
        if (await _db.EventCategories.AnyAsync(ec => ec.EventId == eventId && ec.CategoryId == categoryId))
            return Conflict("Category is already attached to this event.");
        _db.EventCategories.Add(new EventCategory { EventId = eventId, CategoryId = categoryId });
        await _db.SaveChangesAsync();
        return StatusCode(201);
    }

    /// <summary>Removes a category from an event the Organiser owns. Blocked if participants have chosen it.</summary>
    [HttpDelete("events/{eventId}/categories/{categoryId}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Unlink(int eventId, int categoryId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();
        var link = await _db.EventCategories.FindAsync(eventId, categoryId);
        if (link == null) return NotFound();
        if (await _db.Enrolments.AnyAsync(en => en.EventId == eventId && en.CategoryId == categoryId))
            return Conflict("Participants have enrolled in this category.");
        _db.EventCategories.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}