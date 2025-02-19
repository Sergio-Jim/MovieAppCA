using MovieApp.Application.DTOs;

namespace MovieApp.Application.Interfaces
{
    public interface IIdentityService
    {
        Task<(bool Succeeded, string[] Errors)> LoginAsync(LoginDTO loginDTO);
        Task<(bool Succeeded, string[] Errors)> RegisterAsync(string email, string password);
        Task LogoutAsync();
    }
}