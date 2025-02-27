using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _webHostEnvironment; // to access wwwroot

        public MoviesController(IMovieRepository movieRepository, IAuditService auditService, UserManager<User> userManager, SignInManager<User> signInManager, ILogger<UserManagementController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _movieRepository = movieRepository;
            _auditService = auditService;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
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
                    m.Price.ToString().Contains(searchValue) ||
                    (m.ImageUrl != null && m.ImageUrl.ToLower().Replace("-", " ").Replace(" ", "").Contains(searchValue)));
            }

            recordsTotal = moviesQuery.Count();

            var data = moviesQuery
                .Skip(skip)
                .Take(pageSize)
                .Select(m => new
                {
                    id = m.Id,
                    title = m.Title,
                    releaseDate = m.ReleaseDate.ToString("yyyy-MM-dd"),
                    genre = m.Genre,
                    price = m.Price,
                    rating = m.Rating,
                    imageUrl = m.ImageUrl // Add ImageUrl to the response
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
        public async Task<IActionResult> Create(Movie movie, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    movie.ImageUrl = await SaveImageAsync(imageFile);
                }

                var createdMovie = await _movieRepository.CreateAsync(movie);
                var user = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(user.Id, "CreateMovie", "Movie", createdMovie.Id,
                    $"Created movie: {movie.Title}", null, movie);
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ReleaseDate,Genre,Price,Rating")] Movie movie, IFormFile imageFile = null, string removeImage = null)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            if (TryValidateModel(movie))
            {
                var existingMovie = await _movieRepository.GetByIdAsync(id);
                if (existingMovie == null)
                {
                    return NotFound();
                }

                // Handle image update or removal
                if (imageFile != null && imageFile.Length > 0)
                {
                    _logger.LogInformation("Uploading new image for movie ID {Id}: {FileName}", movie.Id, imageFile.FileName);
                    movie.ImageUrl = await SaveImageAsync(imageFile);
                    if (!string.IsNullOrEmpty(existingMovie.ImageUrl))
                    {
                        _logger.LogInformation("Deleting old image: {ImageUrl}", existingMovie.ImageUrl);
                        DeleteImage(existingMovie.ImageUrl);
                    }
                }
                else if (!string.IsNullOrEmpty(removeImage) && removeImage == "true")
                {
                    _logger.LogInformation("Removing image for movie ID {Id}: {RemoveImage}", movie.Id, removeImage);
                    if (!string.IsNullOrEmpty(existingMovie.ImageUrl))
                    {
                        DeleteImage(existingMovie.ImageUrl);
                    }
                    movie.ImageUrl = null;
                }
                else
                {
                    // Keep existing image
                    movie.ImageUrl = existingMovie.ImageUrl;
                }

                // Update movie properties
                existingMovie.Title = movie.Title;
                existingMovie.ReleaseDate = movie.ReleaseDate;
                existingMovie.Genre = movie.Genre;
                existingMovie.Price = movie.Price;
                existingMovie.Rating = movie.Rating;
                existingMovie.ImageUrl = movie.ImageUrl;

                try
                {
                    await _movieRepository.UpdateAsync(existingMovie);
                    var user = await _userManager.GetUserAsync(User);
                    await _auditService.LogAsync(user.Id, "UpdateMovie", "Movie", movie.Id,
                        $"Updated movie: {movie.Title} (Image: {movie.ImageUrl ?? "None"})", existingMovie, movie);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update movie ID {Id}", movie.Id);
                    return View(movie);
                }
            }
            _logger.LogWarning("Model validation failed for editing movie ID {Id}: {Errors}", movie.Id, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
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

            // Keep the image in wwwroot/images/movies (do not delete)
            await _auditService.LogAsync(user.Id, "DeleteMovie", "Movie", id,
                $"Deleted movie: {movie.Title}", movie, null);
            await _movieRepository.DeleteAsync(id);

            _logger.LogInformation("Movie ID {MovieId} deleted successfully by User ID {UserId}", id, user.Id);
            return Json(new { success = true, message = "Movie deleted successfully." });
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "movies");
            Directory.CreateDirectory(uploadsFolder); // Ensure directory exists

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // Return relative path for storage in ImageUrl
            return $"/images/movies/{uniqueFileName}";
        }

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            // Remove leading slash if present (e.g., "/images/movies/filename.jpg" -> "images/movies/filename.jpg")
            string filePath = imageUrl.TrimStart('/');
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, filePath);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
