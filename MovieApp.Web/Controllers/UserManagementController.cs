using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieApp.Application.DTOs;
using MovieApp.Application.Interfaces;
using Serilog;

namespace MovieApp.Web.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class UserManagementController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            IUserManagementService userManagementService,
            ILogger<UserManagementController> logger)
        {
            _userManagementService = userManagementService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("SuperAdmin accessing user management page");
            var users = await _userManagementService.GetAllUsersAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateUserRoleDTO updateRoleDto)
        {
            _logger.LogInformation("SuperAdmin updating role for user ID {UserId} to {NewRole}",
                updateRoleDto.UserId, updateRoleDto.NewRole);
            var result = await _userManagementService.UpdateUserRoleAsync(updateRoleDto);
            if (result)
            {
                _logger.LogInformation("Role update successful for user ID {UserId}", updateRoleDto.UserId);
            }
            else
            {
                _logger.LogWarning("Role update failed for user ID {UserId}", updateRoleDto.UserId);
            }
            return Json(new { success = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            _logger.LogInformation("SuperAdmin attempting to delete user ID {UserId}", userId);
            var result = await _userManagementService.DeleteUserAsync(userId);
            if (result)
            {
                _logger.LogInformation("User ID {UserId} deleted successfully", userId);
            }
            else
            {
                _logger.LogWarning("Failed to delete user ID {UserId}", userId);
            }
            return Json(new { success = result });
        }
    }
}