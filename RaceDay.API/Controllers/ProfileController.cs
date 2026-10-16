using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers;

public record ProfileDto(string FirstName, string LastName);

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly RaceDayContext _db;
    public ProfileController(RaceDayContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Gets the signed-in user's profile.</summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var u = await _db.Users.FindAsync(CurrentUserId);
        return u == null ? NotFound() : Ok(new { u.Id, u.FirstName, u.LastName, u.Email, u.Role });
    }

    /// <summary>Updates the signed-in user's first and last name.</summary>
    [HttpPut]
    public async Task<IActionResult> Update(ProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            return BadRequest("Profile data is invalid.");
        var u = await _db.Users.FindAsync(CurrentUserId);
        if (u == null) return NotFound();
        u.FirstName = dto.FirstName;
        u.LastName = dto.LastName;
        await _db.SaveChangesAsync();
        return Ok(new { u.Id, u.FirstName, u.LastName, u.Email, u.Role });
    }
}
