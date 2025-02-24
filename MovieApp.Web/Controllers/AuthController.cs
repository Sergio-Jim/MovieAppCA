using Microsoft.AspNetCore.Mvc;
using MovieApp.Application.DTOs;
using MovieApp.Application.Interfaces;
using MovieApp.Infrastructure.Services;

namespace MovieApp.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IIdentityService _identityService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IIdentityService identityService, ILogger<AuthController> logger)
        {
            _identityService = identityService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            _logger.LogInformation("Displaying login page");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for login attempt");
                return View(model);
            }

            var result = await _identityService.LoginAsync(model);

            if (result.Succeeded)
            {
                _logger.LogInformation("Redirecting to home after successful login");
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            _logger.LogWarning("Login failed: {Errors}", string.Join(", ", result.Errors));
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDTO model)
        {
            if (ModelState.IsValid)
            {
                var result = await _identityService.RegisterViewerAsync(model);
                if (result.Succeeded)
                {
                    // Automatically log in the user after registration
                    await _identityService.LoginAsync(new LoginDTO
                    {
                        Email = model.Email,
                        Password = model.Password,
                        RememberMe = false
                    });

                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _identityService.LogoutAsync();
            return RedirectToAction("Login");
        }
    }
}