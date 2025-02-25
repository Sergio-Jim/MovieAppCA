using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApp.Domain.Entities;
using MovieApp.Infrastructure.Data;
using MovieApp.Web.Models;

[Authorize(Roles = "SuperAdmin")]
public class AuditController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager; // Inject UserManager

    public AuditController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        var viewModel = logs.Select(log => new AuditLogViewModel
        {
            Id = log.Id,
            UserId = log.UserId,
            Action = log.Action ?? "N/A",
            EntityType = log.EntityType ?? "N/A",
            EntityId = log.EntityId,
            PreviousState = FormatState(log.PreviousState),
            CurrentState = FormatState(log.CurrentState),
            Details = log.Details ?? "N/A",
            Timestamp = log.Timestamp
        }).ToList();

        return View(viewModel);
    }

    private string FormatState(string stateJson)
    {
        if (string.IsNullOrEmpty(stateJson)) return "N/A";
        try
        {
            var state = System.Text.Json.JsonSerializer.Deserialize<object>(stateJson);
            if (state is Movie movie)
            {
                return $"<pre>Title: {movie.Title}, Genre: {movie.Genre}, Price: {movie.Price}, Rating: {movie.Rating}, ReleaseDate: {movie.ReleaseDate}</pre>";
            }
            else if (state is User user)
            {
                var roles = _userManager.GetRolesAsync(user).Result; // Use Result for simplicity; consider async in real app
                return $"<pre>Email: {user.Email}, Roles: {string.Join(", ", roles)}, FirstName: {user.FirstName}, LastName: {user.LastName}</pre>";
            }
            else if (state is IList<string> roles)
            {
                return $"<pre>Roles: {string.Join(", ", roles)}</pre>";
            }
            return $"<pre>{stateJson}</pre>";
        }
        catch (Exception)
        {
            return $"<pre>{stateJson}</pre>";
        }
    }
}