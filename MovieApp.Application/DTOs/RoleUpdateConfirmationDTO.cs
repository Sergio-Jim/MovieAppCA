using System.ComponentModel.DataAnnotations;

namespace MovieApp.Application.DTOs
{
    public class RoleUpdateConfirmationDTO
    {
        public int UserId { get; set; }
        public string NewRole { get; set; }
        public string SuperAdminEmail { get; set; }
        [DataType(DataType.Password)]
        public string SuperAdminPassword { get; set; }
    }
}