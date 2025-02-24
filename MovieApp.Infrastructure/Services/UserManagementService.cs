using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieApp.Application.DTOs;
using MovieApp.Application.Interfaces;
using MovieApp.Domain.Entities;
using Serilog;
using Serilog.Core;

namespace MovieApp.Infrastructure.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager, 
            ILogger<UserManagementService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<IEnumerable<UserManagementDTO>> GetAllUsersAsync()
        {
            _logger.LogInformation("Retrieving all users");
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserManagementDTO>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserManagementDTO
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CurrentRole = roles.FirstOrDefault() ?? "No Role",
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                });
            }
            _logger.LogInformation("Retrieved {UserCount} users", userDtos.Count);
            return userDtos;
        }

        public async Task<bool> UpdateUserRoleAsync(UpdateUserRoleDTO updateRoleDto)
        {
            _logger.LogInformation("Attempting to update role for user ID {UserId} to {NewRole}",
                updateRoleDto.UserId, updateRoleDto.NewRole);

            var user = await _userManager.FindByIdAsync(updateRoleDto.UserId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", updateRoleDto.UserId);
                return false;
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var result = await _userManager.AddToRoleAsync(user, updateRoleDto.NewRole);
            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully updated role for user ID {UserId} to {NewRole}",
                    updateRoleDto.UserId, updateRoleDto.NewRole);
                return true;
            }

            _logger.LogError("Failed to update role for user ID {UserId}. Errors: {@Errors}",
                updateRoleDto.UserId, result.Errors.Select(e => e.Description));
            return false;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            _logger.LogInformation("Attempting to delete user with ID {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                return false;
            }

            // Check if this is the last SuperAdmin
            if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                if (superAdmins.Count <= 1)
                {
                    _logger.LogWarning("Cannot delete user ID {UserId} - last SuperAdmin", userId);
                    return false; // Cannot delete the last SuperAdmin
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully deleted user with ID {UserId}", userId);
                return true;
            }

            _logger.LogError("Failed to delete user ID {UserId}. Errors: {@Errors}",
                userId, result.Errors.Select(e => e.Description));
            return false;
        }

        public async Task<bool> IsSuperAdminAsync(int userId)
        {
            _logger.LogInformation("Checking if user ID {UserId} is SuperAdmin", userId);
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                return false;
            }

            var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
            _logger.LogInformation("User ID {UserId} is SuperAdmin: {IsSuperAdmin}", userId, isSuperAdmin);
            return isSuperAdmin;
        }
    }
}