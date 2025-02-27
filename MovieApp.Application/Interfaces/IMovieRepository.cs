using MovieApp.Domain.Entities;

namespace MovieApp.Application.Interfaces
{
    public interface IMovieRepository
    {
        Task<Movie> GetByIdAsync(int id);
        Task<Movie> CreateAsync(Movie movie);
        Task UpdateAsync(Movie movie);
        Task DeleteAsync(int id);
        Task<IEnumerable<Movie>> GetAllAsync(); // Ensure this returns Movie with ImageUrl
        Task<IEnumerable<Movie>> GetByGenreAsync(string genre);
    }
}