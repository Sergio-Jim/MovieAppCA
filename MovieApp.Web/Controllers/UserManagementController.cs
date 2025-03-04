﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MovieApp.Application.DTOs;
using MovieApp.Application.Interfaces;
using MovieApp.Domain.Entities;
using MovieApp.Web.Models;
using Serilog;

namespace MovieApp.Web.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class UserManagementController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IAuditService _auditService;
        private readonly ILogger<UserManagementController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public UserManagementController(
        IUserManagementService userManagementService,
        IAuditService auditService,
        ILogger<UserManagementController> logger,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<IdentityRole<int>> roleManager) // Add this parameter
        {
            _userManagementService = userManagementService;
            _auditService = auditService;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager; // Initialize this
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("SuperAdmin accessing user management page");
            var users = await _userManagementService.GetAllUsersAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> EditRoles(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var allRoles = new List<string> { "SuperAdmin", "Admin", "Viewer" };
            foreach (var role in allRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole<int> { Name = role });
                }
            }

            var model = new EditRolesViewModel
            {
                UserId = userId,
                Email = user.Email,
                AvailableRoles = allRoles.Select(role => new RoleCheckbox
                {
                    RoleName = role,
                    IsSelected = currentRoles.Contains(role)
                }).ToList()
            };
            return PartialView("_EditRolesModal", model); // Return partial view for modal
        }

        //-----Edit Roles

        [HttpGet]
        public IActionResult UpdateRole(int userId, string newRole)
        {
            return Json(new { success = true, userId = userId, newRole = newRole });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(RoleUpdateConfirmationDTO model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            var superAdmin = await _userManager.FindByEmailAsync(model.SuperAdminEmail);
            if (superAdmin == null || !await _userManager.IsInRoleAsync(superAdmin, "SuperAdmin"))
            {
                return Json(new { success = false, errors = new[] { "Invalid SuperAdmin email." } });
            }

            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(superAdmin, model.SuperAdminPassword, false);
            if (!passwordCheck.Succeeded)
            {
                return Json(new { success = false, errors = new[] { "Invalid SuperAdmin password." } });
            }

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            var previousRoles = await _userManager.GetRolesAsync(user);
            var newRoles = model.NewRole.Split(",").ToList();
            var result = await _userManagementService.UpdateUserRoleAsync(model.UserId, newRoles);
            if (result)
            {
                await _auditService.LogAsync(superAdmin.Id, "UpdateUserRole", "User", model.UserId,
                    $"Updated roles from {string.Join(", ", previousRoles)} to {model.NewRole}", previousRoles, newRoles);
                _logger.LogInformation("Role update successful for user ID {UserId}", model.UserId);
                return Json(new { success = true, message = "Roles updated successfully." });
            }

            _logger.LogWarning("Role update failed for user ID {UserId}", model.UserId);
            return Json(new { success = false, errors = new[] { "Failed to update roles." } });
        }

        // Add Register User Action
        [HttpGet]
        public IActionResult RegisterUser()
        {
            var model = new RegisterUserViewModel
            {
                AvailableRoles = new List<RoleCheckbox>
            {
                new RoleCheckbox { RoleName = "SuperAdmin" },
                new RoleCheckbox { RoleName = "Admin" },
                new RoleCheckbox { RoleName = "Viewer" }
            }
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterUser(RegisterUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                var selectedRoles = model.AvailableRoles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList();
                foreach (var role in selectedRoles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole<int> { Name = role });
                    }
                    await _userManager.AddToRoleAsync(user, role);
                }
                var superAdmin = await _userManager.GetUserAsync(User);
                var currentState = new
                {
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = selectedRoles
                };
                await _auditService.LogAsync(superAdmin.Id, "RegisterUser", "User", user.Id,
                    $"Registered user with email: {user.Email}", null, currentState);
                // _logger.LogInformation("User {User} registered successfully", User);
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
                // _logger.LogWarning("Failed to register user {User}", User);
            }
            return View(model);
        }

        //Delete user with confirmation
        [HttpGet]
        public IActionResult DeleteUser(int userId)
        {
            return Json(new { success = true, userId = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(UserDeletionConfirmationDTO model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            var superAdmin = await _userManager.FindByEmailAsync(model.SuperAdminEmail);
            if (superAdmin == null || !await _userManager.IsInRoleAsync(superAdmin, "SuperAdmin"))
            {
                return Json(new { success = false, errors = new[] { "Invalid SuperAdmin email." } });
            }

            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(superAdmin, model.SuperAdminPassword, false);
            if (!passwordCheck.Succeeded)
            {
                return Json(new { success = false, errors = new[] { "Invalid SuperAdmin password." } });
            }

            var user = await _userManager.FindByIdAsync(model.UserId.ToString()); // Fetch user before deletion
            var previousRoles = await _userManager.GetRolesAsync(user);
            var previousState = new
            {
                Email = user?.Email,
                FirstName = user?.FirstName,
                LastName = user?.LastName,
                Roles = previousRoles
            };
            var result = await _userManagementService.DeleteUserAsync(model.UserId);
            if (result)
            {
                await _auditService.LogAsync(superAdmin.Id, "DeleteUser", "User", model.UserId,
                    $"Deleted user: {user?.Email ?? "Unknown"}", previousState, null);
                _logger.LogInformation("User ID {UserId} deleted successfully", model.UserId);
                return Json(new { success = true, message = "User deleted successfully." });
            }

            _logger.LogWarning("Failed to delete user ID {UserId}", model.UserId);
            return Json(new { success = false, errors = new[] { "Failed to delete user." } });
        }
    }
}