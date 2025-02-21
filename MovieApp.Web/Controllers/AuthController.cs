using Microsoft.AspNetCore.Mvc;
using MovieApp.Application.DTOs;
using MovieApp.Application.Interfaces;

namespace MovieApp.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IIdentityService _identityService;

        public AuthController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _identityService.LoginAsync(model);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

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