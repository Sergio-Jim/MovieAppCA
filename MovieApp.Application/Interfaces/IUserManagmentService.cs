using MovieApp.Application.DTOs;

namespace MovieApp.Application.Interfaces
{
    public interface IUserManagementService
    {
        Task<IEnumerable<UserManagementDTO>> GetAllUsersAsync();
        Task<bool> UpdateUserRoleAsync(int userId, List<string> newRoles);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> IsSuperAdminAsync(int userId);
    }
}