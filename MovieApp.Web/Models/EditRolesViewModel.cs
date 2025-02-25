namespace MovieApp.Web.Models
{
    public class EditRolesViewModel
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public List<RoleCheckbox> AvailableRoles { get; set; }
    }
}
