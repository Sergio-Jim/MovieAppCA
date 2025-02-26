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
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<UserManagementController> _logger;

        public MoviesController(IMovieRepository movieRepository, IAuditService auditService, UserManager<User> userManager, SignInManager<User> signInManager, ILogger<UserManagementController> logger)
        {
            _movieRepository = movieRepository;
            _auditService = auditService;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GetMovies()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();

            int pageSize = length != null ? Convert.ToInt32(length) : 10;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            int recordsTotal = 0;

            var moviesQuery = _movieRepository.GetAllAsync().Result.AsQueryable();

            if (!string.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToLower().Replace("-", " ").Replace(" ", "");
                moviesQuery = moviesQuery.Where(m =>
                    m.Title.ToLower().Replace("-", " ").Replace(" ", "").Contains(searchValue) ||
                    m.Genre.ToLower().Replace("-", " ").Replace(" ", "").Contains(searchValue) ||
                    m.Rating.ToLower().Replace("-", " ").Replace(" ", "").Contains(searchValue) ||
                    m.Price.ToString().Contains(searchValue));
            }

            recordsTotal = moviesQuery.Count();

            var data = moviesQuery
                .Skip(skip)
                .Take(pageSize)
                .Select(m => new
                {
                    id = m.Id, // Ensure id is included
                    title = m.Title,
                    releaseDate = m.ReleaseDate.ToString("yyyy-MM-dd"),
                    genre = m.Genre,
                    price = m.Price,
                    rating = m.Rating
                }).ToList();

            return Json(new
            {
                draw = draw,
                recordsTotal = recordsTotal,
                recordsFiltered = recordsTotal,
                data = data
            });
        }

        // Watching Movie
        [HttpPost]
        [Authorize]
        public IActionResult Watch(int id)
        {
            var movie = _movieRepository.GetByIdAsync(id).Result;
            if (movie == null)
            {
                return Json(new { success = false, errors = new[] { "Movie not found." } });
            }

            // Add your watch logic here (e.g., update user history, log activity)
            return Json(new { success = true, message = "Movie watched successfully." });
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Watch(int id)
        //{
        //    var movie = await _movieRepository.GetByIdAsync(id);
        //    if (movie == null)
        //    {
        //        return NotFound();
        //    }
        //    return Content($"Watching {movie.Title}");
        //}

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // Restrict Create, Edit, Delete to Admin and SuperAdmin role only
        // GET: Movies/Edit/{id} (Admin and SuperAdmin only)
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Edit/{id} (Admin and SuperAdmin only)
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

        // GET: Movies/Edit/{id} (Admin and SuperAdmin only)
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
                var existingMovie = await _movieRepository.GetByIdAsync(id);
                if (existingMovie == null)
                {
                    return NotFound();
                }

                existingMovie.Title = movie.Title;
                existingMovie.ReleaseDate = movie.ReleaseDate;
                existingMovie.Genre = movie.Genre;
                existingMovie.Price = movie.Price;
                existingMovie.Rating = movie.Rating;

                await _movieRepository.UpdateAsync(existingMovie);
                var user = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(user.Id, "UpdateMovie", "Movie", movie.Id,
                    $"Updated movie: {movie.Title}", movie, existingMovie);
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public IActionResult Delete(int id)
        {
            return Json(new { success = true, movieId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Delete(int id, string superAdminEmail, string superAdminPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || (!await _userManager.IsInRoleAsync(user, "Admin") && !await _userManager.IsInRoleAsync(user, "SuperAdmin")))
            {
                return Json(new { success = false, errors = new[] { "Unauthorized access. Only Admins or SuperAdmins can delete movies." } });
            }

            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, superAdminPassword, false);
            if (!passwordCheck.Succeeded)
            {
                return Json(new { success = false, errors = new[] { "Invalid SuperAdmin password." } });
            }

            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
            {
                return Json(new { success = false, errors = new[] { "Movie not found." } });
            }

            await _auditService.LogAsync(user.Id, "DeleteMovie", "Movie", id,
                $"Deleted movie: {movie.Title}", movie, null);
            await _movieRepository.DeleteAsync(id);

            _logger.LogInformation("Movie ID {MovieId} deleted successfully by User ID {UserId}", id, user.Id);
            return Json(new { success = true, message = "Movie deleted successfully." });
        }
    }
}
