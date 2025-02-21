using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MovieApp.Application.DTOs;
using MovieApp.Application.Interfaces;
using MovieApp.Domain.Entities;

namespace MovieApp.Infrastructure.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public UserManagementService(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IEnumerable<UserManagementDTO>> GetAllUsersAsync()
        {
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

            return userDtos;
        }

        public async Task<bool> UpdateUserRoleAsync(UpdateUserRoleDTO updateRoleDto)
        {
            var user = await _userManager.FindByIdAsync(updateRoleDto.UserId.ToString());
            if (user == null) return false;

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var result = await _userManager.AddToRoleAsync(user, updateRoleDto.NewRole);
            return result.Succeeded;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            // Check if this is the last SuperAdmin
            if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                if (superAdmins.Count <= 1)
                {
                    return false; // Cannot delete the last SuperAdmin
                }
            }

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> IsSuperAdminAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            return await _userManager.IsInRoleAsync(user, "SuperAdmin");
        }
    }
}