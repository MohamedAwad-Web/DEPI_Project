namespace Bookify.Core.Models
{
    public class RoomDetailsViewModel
    {
        public Room Room { get; set; } = new Room();
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
    }
}