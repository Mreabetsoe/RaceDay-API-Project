
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : Controller
{
    private readonly ApiService _api;

    public EventsController(ApiService api)
    {
        _api = api;
    }

    private bool IsLoggedIn =>
        !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));

    private bool IsOrganiser =>
        HttpContext.Session.GetString("Role") == "Organiser";

    public async Task<IActionResult> Index()
    {
        var response = await _api.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "api/events"));

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = "Unable to load events.";
            return View(new List<EventViewModel>());
        }

        var events = await response.Content
            .ReadFromJsonAsync<List<EventViewModel>>()
            ?? new List<EventViewModel>();

        return View(events);
    }

    public async Task<IActionResult> Details(int id)
    {
        var response = await _api.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, $"api/events/{id}"));

        if (!response.IsSuccessStatusCode)
            return NotFound();

        var eventItem = await response.Content
            .ReadFromJsonAsync<EventViewModel>();

        if (eventItem == null)
            return NotFound();

        return View(eventItem);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!IsLoggedIn)
            return RedirectToAction("Login", "Auth");

        if (!IsOrganiser)
            return Forbid();

        return View(new EventViewModel
        {
            EventDate = DateOnly.FromDateTime(DateTime.Today)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventViewModel model)
    {
        if (!IsLoggedIn)
            return RedirectToAction("Login", "Auth");

        if (!IsOrganiser)
            return Forbid();

        if (!ModelState.IsValid)
            return View(model);

        var dto = new
        {
            model.Name,
            model.Description,
            model.EventDate,
            model.Location,
            model.DistanceKm,
            model.Capacity,
            model.EventType
        };

        var request = new HttpRequestMessage(
            HttpMethod.Post, "api/events")
        {
            Content = JsonContent.Create(dto)
        };

        var response = await _api.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("",
                "Could not create the event. Please check your details.");
            return View(model);
        }

        TempData["Success"] = "Event created successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsLoggedIn)
            return RedirectToAction("Login", "Auth");

        if (!IsOrganiser)
            return Forbid();

        var response = await _api.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, $"api/events/{id}"));

        if (!response.IsSuccessStatusCode)
            return NotFound();

        var eventItem = await response.Content
            .ReadFromJsonAsync<EventViewModel>();

        return eventItem == null ? NotFound() : View(eventItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventViewModel model)
    {
        if (!IsLoggedIn)
            return RedirectToAction("Login", "Auth");

        if (!IsOrganiser)
            return Forbid();

        if (!ModelState.IsValid)
            return View(model);

        var dto = new
        {
            model.Name,
            model.Description,
            model.EventDate,
            model.Location,
            model.DistanceKm,
            model.Capacity,
            model.EventType
        };

        var request = new HttpRequestMessage(
            HttpMethod.Put, $"api/events/{id}")
        {
            Content = JsonContent.Create(dto)
        };

        var response = await _api.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("",
                "Could not update this event.");
            return View(model);
        }

        TempData["Success"] = "Event updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsLoggedIn)
            return RedirectToAction("Login", "Auth");

        if (!IsOrganiser)
            return Forbid();

        var response = await _api.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"api/events/{id}"));

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Event deleted successfully.";
        else
            TempData["Error"] =
                "This event could not be deleted. It may already have participants.";

        return RedirectToAction(nameof(Index));
    }
}
