using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class RaceDayWebFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // A separate EF Core setup that only knows about the in-memory database
            var efInMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Remove the real SQL Server database setup
            var old = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RaceDayContext>));
            if (old != null)
            {
                services.Remove(old);
            }

            // Add the in-memory database for tests
            services.AddDbContext<RaceDayContext>(o =>
                o.UseInMemoryDatabase(_dbName)
                 .UseInternalServiceProvider(efInMemoryProvider));
        });
    }

    public void SeedUser(string email, string password, string role)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
        if (db.Users.Any(u => u.Email == email)) return;
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var user = new User { FirstName = "Test", LastName = role, Email = email, Role = role };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        db.SaveChanges();
    }
}

public class ApiSecurityTests : IClassFixture<RaceDayWebFactory>
{
    private const string OrgEmail = "org@test.com";
    private const string OrgPass = "Org123!";
    private const string PartEmail = "part@test.com";
    private const string PartPass = "Part123!";

    private readonly RaceDayWebFactory _factory;

    public ApiSecurityTests(RaceDayWebFactory factory)
    {
        _factory = factory;
        _factory.SeedUser(OrgEmail, OrgPass, "Organiser");
        _factory.SeedUser(PartEmail, PartPass, "Participant");
    }

    private async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    private static object EventBody(string name) => new
    {
        Name = name,
        Description = "Test event",
        EventDate = "2026-12-01",
        Location = "Pretoria",
        DistanceKm = 10,
        Capacity = 100,
        EventType = "Run"
    };

    [Fact]
    public async Task Unauthenticated_CannotReadProfile_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CannotCreateEvent_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/events", EventBody("Anon"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_Participant_Returns201()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { FirstName = "Ann", LastName = "Lee", Email = $"{Guid.NewGuid()}@test.com", Password = "Pass123!", Role = "Participant" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithoutInviteCode_Returns403()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { FirstName = "Ann", LastName = "Lee", Email = $"{Guid.NewGuid()}@test.com", Password = "Pass123!", Role = "Organiser" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { FirstName = "Ann", LastName = "Lee", Email = PartEmail, Password = "Pass123!", Role = "Participant" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { Email = PartEmail, Password = "WrongPass" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var part = await LoginAsync(PartEmail, PartPass);
        var response = await part.PostAsJsonAsync("/api/events", EventBody("Blocked"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory_Returns403()
    {
        var part = await LoginAsync(PartEmail, PartPass);
        var response = await part.PostAsJsonAsync("/api/categories", new { Name = "Blocked", Description = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewOrganiserEnrolmentList_Returns403()
    {
        var part = await LoginAsync(PartEmail, PartPass);
        var response = await part.GetAsync("/api/events/1/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var org = await LoginAsync(OrgEmail, OrgPass);
        var response = await org.PostAsJsonAsync("/api/events", EventBody("Organiser Run"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanEnrolAndEnrolmentIsRecorded()
    {
        var org = await LoginAsync(OrgEmail, OrgPass);
        var part = await LoginAsync(PartEmail, PartPass);

        var catResponse = await org.PostAsJsonAsync("/api/categories",
            new { Name = "Cat-" + Guid.NewGuid(), Description = "test" });
        Assert.Equal(HttpStatusCode.Created, catResponse.StatusCode);
        var catId = (await catResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var evResponse = await org.PostAsJsonAsync("/api/events", EventBody("Enrol Test"));
        Assert.Equal(HttpStatusCode.Created, evResponse.StatusCode);
        var eventId = (await evResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var linkResponse = await org.PostAsync($"/api/events/{eventId}/categories/{catId}", null);
        Assert.Equal(HttpStatusCode.Created, linkResponse.StatusCode);

        var enrolResponse = await part.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { CategoryId = catId });
        Assert.Equal(HttpStatusCode.Created, enrolResponse.StatusCode);

        var mine = await part.GetFromJsonAsync<JsonElement>("/api/enrolments");
        Assert.Contains(mine.EnumerateArray(), e =>
            e.GetProperty("eventId").GetInt32() == eventId && e.GetProperty("categoryId").GetInt32() == catId);
    }
}