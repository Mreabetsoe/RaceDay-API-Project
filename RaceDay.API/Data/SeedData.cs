using Microsoft.AspNetCore.Identity;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public static class SeedData
{
    public static void Initialise(RaceDayContext db, IPasswordHasher<User> hasher)
    {
        if (db.Users.Any()) return;

        var org = new User { FirstName = "Thabo", LastName = "Mokoena", Email = "thabo@raceday.co.za", Role = "Organiser" };
        org.PasswordHash = hasher.HashPassword(org, "Organiser123!");

        var part = new User { FirstName = "Lerato", LastName = "Maseko", Email = "lerato@gmail.com", Role = "Participant" };
        part.PasswordHash = hasher.HashPassword(part, "Participant123!");

        db.Users.AddRange(org, part);
        db.Categories.AddRange(
            new Category { Name = "Running", Description = "Road and distance running events" },
            new Category { Name = "Walking", Description = "Walking and recreational walking events" },
            new Category { Name = "Cycling", Description = "Road and recreational cycling events" });
        db.SaveChanges();
    }
}
