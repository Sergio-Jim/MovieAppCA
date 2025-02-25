namespace MovieApp.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(int userId, string action, string entityType, int? entityId, string details);
    }
}