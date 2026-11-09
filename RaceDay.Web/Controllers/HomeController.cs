using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly EventsApiService _events;

    public HomeController(EventsApiService events) => _events = events;

    public async Task<IActionResult> Index()
    {
        try
        {
            var list = await _events.GetEventsAsync();
            return View(list);
        }
        catch (HttpRequestException)
        {
            ViewBag.Error = "We could not reach the RaceDay API. Make sure it is running.";
            return View(new List<EventSummary>());
        }
    }

    public IActionResult Error()
    {
        return View();
    }
}