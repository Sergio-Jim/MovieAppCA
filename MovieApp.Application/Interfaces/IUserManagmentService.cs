using MovieApp.Application.DTOs;

namespace MovieApp.Application.Interfaces
{
    public interface IUserManagementService
    {
        Task<IEnumerable<UserManagementDTO>> GetAllUsersAsync();
        Task<bool> UpdateUserRoleAsync(UpdateUserRoleDTO updateRoleDto);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> IsSuperAdminAsync(int userId);
    }
}