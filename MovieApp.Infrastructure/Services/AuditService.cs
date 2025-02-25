﻿using Microsoft.Extensions.Logging;
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

        public async Task LogAsync(int userId, string action, string entityType, int? entityId, string details)
        {
            var truncatedDetails = details.Length > 500 ? details.Substring(0, 500) : details;
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = truncatedDetails,
                Timestamp = DateTime.UtcNow
            };
            try
            {
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Audit log recorded: User {UserId} performed {Action} on {EntityType} (ID: {EntityId}) - {Details}",
                    userId, action, entityType, entityId, truncatedDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save audit log");
            }
        }
    }
}