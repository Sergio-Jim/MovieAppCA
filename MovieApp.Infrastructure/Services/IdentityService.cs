using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieApp.Application.DTOs;
using MovieApp.Application.Interfaces;
using MovieApp.Domain.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MovieApp.Infrastructure.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<IdentityService> _logger;

        public IdentityService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<IdentityService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

    public async Task<(bool Succeeded, string[] Errors)> LoginAsync(LoginDTO loginDTO)
        {
            var user = await _userManager.FindByEmailAsync(loginDTO.Email);

        if (user == null)
        {
            _logger.LogWarning("User not found for email: {Email}", loginDTO.Email);
            return (false, new[] { "User not found" });
        }

        var result = await _signInManager.PasswordSignInAsync(
                user,
                loginDTO.Password,
                loginDTO.RememberMe,
                false);

            if (result.Succeeded)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("User {Email} logged in successfully", loginDTO.Email);
                return (true, Array.Empty<string>());
            }

            _logger.LogWarning("Invalid login attempt for email: {Email}", loginDTO.Email);
            return (false, new[] { "Invalid login attempt" });
        }

        public async Task<(bool Succeeded, string[] Errors)> RegisterViewerAsync(RegisterDTO registerDto)
        {
            _logger.LogInformation("Register attempt for email: {Email}", registerDto.Email);
            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true // For simplicity, you might want to add email confirmation later
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} registered successfully", registerDto.Email);
                await _userManager.AddToRoleAsync(user, "Viewer");
                return (true, Array.Empty<string>());
            }

            _logger.LogError("Failed to register user {Email}. Errors: {@Errors}", registerDto.Email, result.Errors);
            return (false, result.Errors.Select(e => e.Description).ToArray());
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out");
        }
    }
}