namespace Bookify.Core.Models
{
    public class ReservationCart
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfNights => (CheckOutDate - CheckInDate).Days;
        public decimal TotalPrice => PricePerNight * NumberOfNights;
    }
}