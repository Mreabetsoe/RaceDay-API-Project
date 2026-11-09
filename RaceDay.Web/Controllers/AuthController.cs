
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AuthController : Controller
{
    private readonly EventsApiService _apiService;

    public AuthController(EventsApiService apiService)
    {
        _apiService = apiService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var request = new HttpRequestMessage(
            HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(model)
        };

        var response = await _apiService.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(
                "",
                "Login failed. Please check your email and password."
            );

            return View(model);
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

        if (result == null)
        {
            ModelState.AddModelError("", "Unable to read login response.");
            return View(model);
        }

        HttpContext.Session.SetString("UserId", result.Id.ToString());
        HttpContext.Session.SetString("Email", result.Email ?? model.Email);
        HttpContext.Session.SetString("Role", result.Role ?? "");

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (model.Role != "Participant" && model.Role != "Organiser")
        {
            ModelState.AddModelError("", "Invalid account type.");
            return View(model);
        }

        var request = new HttpRequestMessage(
            HttpMethod.Post, "api/auth/register")
        {
            Content = JsonContent.Create(model)
        };

        var response = await _apiService.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(
                "",
                "Registration failed. Please check your details and try again."
            );

            return View(model);
        }

        TempData["Success"] = "Registration successful. Please log in.";

        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post, "api/auth/logout");

        await _apiService.SendAsync(request);

        HttpContext.Session.Clear();

        return RedirectToAction("Login");
    }

    private class AuthResponse
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}

