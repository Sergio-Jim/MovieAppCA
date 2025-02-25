using System.Text.Json;
using Microsoft.Extensions.Logging;
using MovieApp.Application.Interfaces;
using MovieApp.Domain.Entities;
using MovieApp.Infrastructure.Data;

namespace MovieApp.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(int userId, string action, string entityType, int? entityId,
    string details, object previousState = null, object currentState = null)
        {
            var truncatedDetails = details ?? ""; // Default to empty string if null
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action ?? "", // Default to empty string if null
                EntityType = entityType ?? "", // Default to empty string if null
                EntityId = entityId,
                PreviousState = previousState != null ? JsonSerializer.Serialize(previousState) : null,
                CurrentState = currentState != null ? JsonSerializer.Serialize(currentState) : null,
                Details = truncatedDetails.Length > 500 ? truncatedDetails.Substring(0, 500) : truncatedDetails,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Audit log recorded: User {UserId} performed {Action} on {EntityType} (ID: {EntityId}) - {Details}",
                    userId, action ?? "", entityType ?? "", entityId, truncatedDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save audit log");
                throw;
            }
        }
    }
}