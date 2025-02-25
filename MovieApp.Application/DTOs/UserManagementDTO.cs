namespace MovieApp.Application.DTOs
{
    public class UserManagementDTO
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<string> CurrentRoles { get; set; } = new List<string>(); // List of assigned roles
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class UpdateUserRoleDTO
    {
        public int UserId { get; set; }
        public string NewRole { get; set; }
    }
}