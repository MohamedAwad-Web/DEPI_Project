using Bookify.Core.Models;

namespace Bookify.Core.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int TotalBookings { get; set; }
        public IEnumerable<Room> RecentRooms { get; set; } = new List<Room>();
        public IEnumerable<Booking> RecentBookings { get; set; } = new List<Booking>();
    }
}