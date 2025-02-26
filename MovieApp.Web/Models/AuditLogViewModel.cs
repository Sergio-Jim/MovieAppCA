namespace MovieApp.Web.Models
{
    public class AuditLogViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public string PreviousState { get; set; }
        public string CurrentState { get; set; }
        public string PreviousStateJson { get; set; } // Raw JSON for search
        public string CurrentStateJson { get; set; }   // Raw JSON for search
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}