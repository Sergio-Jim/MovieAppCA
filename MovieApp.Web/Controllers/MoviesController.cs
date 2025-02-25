using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MovieApp.Application.Interfaces;
using MovieApp.Domain.Entities;
using MovieApp.Infrastructure.Services;

namespace MovieApp.Web.Controllers
{
    [Authorize] // This ensures the user is logged in
    public class MoviesController : Controller
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IAuditService _auditService;
        private readonly UserManager<User> _userManager;

        public MoviesController(IMovieRepository movieRepository, IAuditService auditService, UserManager<User> userManager)
        {
            _movieRepository = movieRepository;
            _auditService = auditService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var movies = await _movieRepository.GetAllAsync();
            return View(movies);
        }

        // Watching Movie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Watch(int id)
        {
            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return Content($"Watching {movie.Title}");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // Restrict Create, Edit, Delete to Admin role only
        // GET: Movies/Edit/{id} (Admin only)
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Edit/{id} (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Create([Bind("Title,ReleaseDate,Genre,Price,Rating")] Movie movie)
        {
            if (ModelState.IsValid)
            {
                var createdMovie = await _movieRepository.CreateAsync(movie);
                var user = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(user.Id, "CreateMovie", "Movie", createdMovie.Id,
                    $"Created movie: {movie.Title}", null, movie); // null for previous state, movie as current state
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Edit/{id} (Admin only)
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ReleaseDate,Genre,Price,Rating")] Movie movie)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingMovie = await _movieRepository.GetByIdAsync(id); // Fetch the tracked movie
                if (existingMovie == null)
                {
                    return NotFound();
                }

                // Update the tracked entity with the new values
                existingMovie.Title = movie.Title;
                existingMovie.ReleaseDate = movie.ReleaseDate;
                existingMovie.Genre = movie.Genre;
                existingMovie.Price = movie.Price;
                existingMovie.Rating = movie.Rating;

                await _movieRepository.UpdateAsync(existingMovie); // Update the tracked entity
                var user = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(user.Id, "UpdateMovie", "Movie", movie.Id,
                    $"Updated movie: {movie.Title}", movie, existingMovie); // Previous state is the old values, current is the new
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // POST: Movies/Delete/{id} (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie != null)
            {
                var user = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(user.Id, "DeleteMovie", "Movie", id,
                    $"Deleted movie: {movie.Title}", movie, null); // Previous state (movie), no current state
                await _movieRepository.DeleteAsync(id);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
