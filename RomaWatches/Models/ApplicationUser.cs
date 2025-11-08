using Microsoft.AspNetCore.Identity;

namespace RomaWatches.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = "user"; // "admin" or "user"
    }
}

