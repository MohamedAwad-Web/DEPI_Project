using Microsoft.AspNetCore.Identity;

namespace Bookify.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>(); // Added virtual
    }
}