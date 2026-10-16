using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class EventsControllerTests
{
    private static RaceDayContext NewDb()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new RaceDayContext(options);
    }

    private static EventsController ControllerAs(RaceDayContext db, int userId, string role)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, role) };
        var controller = new EventsController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
            }
        };
        return controller;
    }

    private static EventDto ValidDto() =>
        new("Test 10K", "A test event", new DateOnly(2026, 12, 1), "Pretoria", 10m, 100, "Run");

    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        using var db = NewDb();
        var controller = ControllerAs(db, 1, "Organiser");

        var result = await controller.Create(ValidDto());

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Create_InvalidEventType_ReturnsBadRequest()
    {
        using var db = NewDb();
        var controller = ControllerAs(db, 1, "Organiser");
        var dto = ValidDto() with { EventType = "Swim" };

        var result = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent()
    {
        using var db = NewDb();
        db.Events.Add(new Event
        {
            Id = 1,
            OrganiserId = 2,
            Name = "X",
            Description = "X",
            Location = "X",
            EventDate = new DateOnly(2026, 12, 1),
            DistanceKm = 5,
            Capacity = 10,
            EventType = "Run"
        });
        db.SaveChanges();
        var controller = ControllerAs(db, 1, "Organiser");

        var result = await controller.Update(1, ValidDto());

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetById_MissingEvent_ReturnsNotFound()
    {
        using var db = NewDb();
        var controller = ControllerAs(db, 1, "Organiser");

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundResult>(result);
    }
}