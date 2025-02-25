using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MovieApp.Application.DTOs;
using MovieApp.Application.Interfaces;
using MovieApp.Domain.Entities;
using MovieApp.Infrastructure.Services;

namespace MovieApp.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IIdentityService _identityService;
        private readonly IAuditService _auditService; // Added audit interface
        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<User> _userManager;

        public AuthController(
            IIdentityService identityService, 
            IAuditService auditService,
            ILogger<AuthController> logger, 
            UserManager<User> userManager)
        {
            _identityService = identityService;
            _auditService = auditService;
            _logger = logger;
            _userManager = userManager;
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
                var user = await _userManager.FindByEmailAsync(model.Email);
                await _auditService.LogAsync(user.Id, "Login", "User", user.Id, $"User {model.Email} logged in");
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
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    await _auditService.LogAsync(user.Id, "Register", "User", user.Id, $"User {model.Email} registered");
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
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogAsync(user.Id, "Logout", "User", user.Id, $"User {user.Email} logged out");
            }
            await _identityService.LogoutAsync();
            return RedirectToAction("Login");
        }
    }
}