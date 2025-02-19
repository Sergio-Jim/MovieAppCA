using MovieApp.Domain.Entities;

namespace MovieApp.Application.Interfaces
{
    public interface IMovieRepository
    {
        Task<IEnumerable<Movie>> GetAllAsync();
        Task<Movie> GetByIdAsync(int id);
        Task<Movie> CreateAsync(Movie movie);
        Task UpdateAsync(Movie movie);
        Task DeleteAsync(int id);
        Task<IEnumerable<Movie>> GetByGenreAsync(string genre);
    }
}