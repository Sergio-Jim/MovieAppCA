using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApp.Domain.Entities;
using MovieApp.Infrastructure.Data;
using MovieApp.Web.Models;
using System.Text.Json;

namespace MovieApp.Web.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AuditController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UserManagementController> _logger;

        public AuditController(ApplicationDbContext context, UserManager<User> userManager, ILogger<UserManagementController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GetAuditLogs()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();

            int pageSize = length != null ? Convert.ToInt32(length) : 10;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            int recordsTotal = 0;

            var auditLogsQuery = _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToLower().Replace("-", " ").Replace(" ", ""); // Normalize hyphens and spaces
                auditLogsQuery = auditLogsQuery.Where(a =>
                    a.Action.ToLower().Replace("-", " ").Replace(" ", "").Contains(searchValue) ||
                    a.EntityType.ToLower().Replace("-", " ").Replace(" ", "").Contains(searchValue) ||
                    a.Details.ToLower().Replace("-", " ").Replace(" ", "").Contains(searchValue) ||
                    a.UserId.ToString().Contains(searchValue) ||
                    (a.EntityId.HasValue && a.EntityId.Value.ToString().Contains(searchValue)) ||
                    (a.PreviousState != null && a.PreviousState.ToLower().Replace("-", " ").Replace(" ", "").Contains(searchValue)) ||
                    (a.CurrentState != null && a.CurrentState.ToLower().Replace("-", " ").Replace(" ", "").Contains(searchValue)));
            }

            recordsTotal = auditLogsQuery.Count();

            var data = auditLogsQuery
                .Skip(skip)
                .Take(pageSize)
                .Select(a => new AuditLogViewModel
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    Action = a.Action ?? "N/A",
                    EntityType = a.EntityType ?? "N/A",
                    EntityId = a.EntityId,
                    PreviousStateJson = a.PreviousState, // Raw JSON for search
                    CurrentStateJson = a.CurrentState,   // Raw JSON for search
                    Details = a.Details ?? "N/A",
                    Timestamp = a.Timestamp
                }).ToList();

            // Format states in memory after retrieval
            var formattedData = data.Select(log => new AuditLogViewModel
            {
                Id = log.Id,
                UserId = log.UserId,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                PreviousState = TruncateAndLinkState(log.PreviousStateJson), // Truncate and link
                CurrentState = TruncateAndLinkState(log.CurrentStateJson),   // Truncate and link
                Details = log.Details,
                Timestamp = log.Timestamp,
                PreviousStateJson = log.PreviousStateJson, // Keep raw JSON for search
                CurrentStateJson = log.CurrentStateJson    // Keep raw JSON for search
            }).ToList();

            return Json(new
            {
                draw = draw,
                recordsTotal = recordsTotal,
                recordsFiltered = recordsTotal,
                data = formattedData
            });
        }

        private string TruncateAndLinkState(string stateJson)
        {
            if (string.IsNullOrEmpty(stateJson)) return "N/A";
            try
            {
                //json serializer changes text from raw json text
                var state = JsonSerializer.Deserialize<object>(stateJson);
                string fullState = "";
                if (state is Movie movie)
                {
                    fullState = $"<pre>Title: {movie.Title}, Genre: {movie.Genre}, Price: {movie.Price}, Rating: {movie.Rating}, ReleaseDate: {movie.ReleaseDate}</pre>";
                }
                else if (state is User user)
                {
                    var roles = _userManager.GetRolesAsync(user).Result;
                    fullState = $"<pre>Email: {user.Email}, Roles: {string.Join(", ", roles)}, FirstName: {user.FirstName}, LastName: {user.LastName}</pre>";
                }
                else if (state is IDictionary<string, object> dict)
                {
                    fullState = $"<pre>{string.Join(", ", dict.Select(kv => $"{kv.Key}: {kv.Value}"))}</pre>";
                }
                else
                {
                    fullState = $"<pre>{stateJson}</pre>";
                }

                // Truncate to a reasonable length (e.g., 50 characters) and add a link to a modal
                string truncated = fullState.Length > 20 ? fullState.Substring(0, 20) + "..." : fullState;
                return $"<a href='#' class='state-link' data-state='{System.Web.HttpUtility.JavaScriptStringEncode(fullState)}'>{truncated}</a>";
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize audit state: {StateJson}", stateJson);
                return "Error: Invalid state data";
            }
        }

        private string FormatState(string stateJson)
        {
            if (string.IsNullOrEmpty(stateJson)) return "N/A";
            try
            {
                var state = JsonSerializer.Deserialize<object>(stateJson);
                if (state is Movie movie)
                {
                    return $"<pre>Title: {movie.Title}, Genre: {movie.Genre}, Price: {movie.Price}, Rating: {movie.Rating}, ReleaseDate: {movie.ReleaseDate}</pre>";
                }
                else if (state is User user)
                {
                    var roles = _userManager.GetRolesAsync(user).Result;
                    return $"<pre>Email: {user.Email}, Roles: {string.Join(", ", roles)}, FirstName: {user.FirstName}, LastName: {user.LastName}</pre>";
                }
                else if (state is IDictionary<string, object> dict)
                {
                    return $"<pre>{string.Join(", ", dict.Select(kv => $"{kv.Key}: {kv.Value}"))}</pre>";
                }
                return $"<pre>{stateJson}</pre>";
            }
            catch (Exception)
            {
                return $"<pre>{stateJson}</pre>";
            }
        }
    }
}