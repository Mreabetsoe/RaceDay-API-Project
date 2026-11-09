
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class ProfileViewModel
{
    public int Id { get; set; }

    [Required]
    public string FirstName { get; set; } = "";

    [Required]
    public string LastName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Role { get; set; } = "";

    public string? ProfilePictureUrl { get; set; }
}

