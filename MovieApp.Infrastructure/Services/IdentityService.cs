﻿using Microsoft.AspNetCore.Identity;
using MovieApp.Application.DTOs;
using MovieApp.Application.Interfaces;
using MovieApp.Domain.Entities;

namespace MovieApp.Infrastructure.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public IdentityService(
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<(bool Succeeded, string[] Errors)> LoginAsync(LoginDTO loginDTO)
        {
            var user = await _userManager.FindByEmailAsync(loginDTO.Email);

            if (user == null)
            {
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
                return (true, Array.Empty<string>());
            }

            return (false, new[] { "Invalid login attempt" });
        }

        public async Task<(bool Succeeded, string[] Errors)> RegisterViewerAsync(RegisterDTO registerDto)
        {
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
                await _userManager.AddToRoleAsync(user, "Viewer");
                return (true, Array.Empty<string>());
            }

            return (false, result.Errors.Select(e => e.Description).ToArray());
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}