using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieApp.Application.DTOs;
using MovieApp.Application.Interfaces;

namespace MovieApp.Web.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class UserManagementController : Controller
    {
        private readonly IUserManagementService _userManagementService;

        public UserManagementController(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManagementService.GetAllUsersAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateUserRoleDTO updateRoleDto)
        {
            var result = await _userManagementService.UpdateUserRoleAsync(updateRoleDto);
            return Json(new { success = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var result = await _userManagementService.DeleteUserAsync(userId);
            return Json(new { success = result });
        }
    }
}