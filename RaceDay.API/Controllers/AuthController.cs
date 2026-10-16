using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

public record RegisterDto(string FirstName, string LastName, string Email, string Password, string Role, string? InviteCode);
public record LoginDto(string Email, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IConfiguration _config;

    public AuthController(RaceDayContext db, IPasswordHasher<User> hasher, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    /// <summary>Registers a user as Participant or Organiser. Organiser sign-up needs the invite code.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName) ||
            string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Please complete all fields.");
        if (dto.Role is not ("Organiser" or "Participant"))
            return BadRequest("Role must be Organiser or Participant.");

        // Guard against a missing config value: null == null must not grant access
        var code = _config["OrganiserInviteCode"];
        if (dto.Role == "Organiser" && (string.IsNullOrEmpty(code) || dto.InviteCode != code))
            return StatusCode(403, "A valid organiser invite code is required.");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict("An account with this email already exists.");

        var user = new User { FirstName = dto.FirstName, LastName = dto.LastName, Email = dto.Email, Role = dto.Role };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return StatusCode(201, new { user.Id, user.Email, user.Role });
    }

    /// <summary>Logs in with email and password. Sets a cookie and stores user ID and role in the server-side session.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            return Unauthorized("Your email or password is wrong.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok(new { user.Id, user.Email, user.Role });
    }

    /// <summary>Logs out and clears the server-side session and the cookie.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}