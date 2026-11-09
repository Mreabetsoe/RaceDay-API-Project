
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class CategoryViewModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    public string? Description { get; set; }
}
