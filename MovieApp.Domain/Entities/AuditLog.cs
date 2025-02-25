namespace MovieApp.Domain.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Who performed the action
        public string Action { get; set; } // What they did (e.g., "UpdateUserRole")
        public string EntityType { get; set; } // What entity was affected (e.g., "User", "Movie")
        public int? EntityId { get; set; } // ID of the affected entity (optional)
        public string Details { get; set; } // Additional info (e.g., "Changed role from Viewer to Admin")
        public DateTime Timestamp { get; set; } // When it happened
    }
}