using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MovieApp.Domain.Entities;
using MovieApp.Web.Models;
using System.Diagnostics;

namespace MovieApp.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        //testing
        [HttpGet]
        public async Task<IActionResult> TestUserCreation([FromServices] UserManager<User> userManager)
        {
            var user = new User
            {
                UserName = "TestUser",
                Email = "test@movieapp.com",
                FirstName = "Test",
                LastName = "User",
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, "Test@123!");
            if (result.Succeeded)
            {
                return Content("User created successfully!");
            }
            return Content($"Failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
