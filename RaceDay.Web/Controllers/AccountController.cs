using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace RaceDay.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _f;
        public AccountController(IHttpClientFactory f) => _f = f;

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var res = await _f.CreateClient("api")
                    .PostAsJsonAsync("api/auth/login", new { email, password });

                if (!res.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Invalid email or password.";
                    return View();
                }

                // Save the API cookie
                if (res.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    var cookie = string.Join("; ", cookies.Select(x => x.Split(';')[0]));
                    HttpContext.Session.SetString("ApiCookie", cookie);
                }

                // Save the role
                var body = await res.Content.ReadFromJsonAsync<JsonElement>();
                string role = "Participant";
                foreach (var p in body.EnumerateObject())
                {
                    if (p.Name.ToLower() == "role") role = p.Value.GetString() ?? "Participant";
                }
                HttpContext.Session.SetString("Role", role);

                return RedirectToAction("Index", "Events");
            }
            catch
            {
                ViewBag.Error = "Could not reach the server. Please try again.";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string name, string email, string password, string role)
        {
            try
            {
                var res = await _f.CreateClient("api")
                    .PostAsJsonAsync("api/auth/register", new { name, email, password, role });

                if (!res.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Registration failed. Check your details and try again.";
                    return View();
                }
                TempData["Success"] = "Account created. Please log in.";
                return RedirectToAction("Login");
            }
            catch
            {
                ViewBag.Error = "Could not reach the server. Please try again.";
                return View();
            }
        }

        public async Task<IActionResult> Logout()
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
                var cookie = HttpContext.Session.GetString("ApiCookie");
                if (!string.IsNullOrEmpty(cookie))
                    req.Headers.TryAddWithoutValidation("Cookie", cookie);
                await _f.CreateClient("api").SendAsync(req);
            }
            catch { }
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}