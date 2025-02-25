using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MovieApp.Domain.Entities
{
    public class User : IdentityUser<int>
    {
        [Required]
        public required string FirstName { get; set; } // Use 'required' instead of nullable
        [Required]
        public required string LastName { get; set; } // Use 'required' instead of nullable
        [Required]
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}