namespace Bookify.Core.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled
        public string? StripePaymentIntentId { get; set; }
        public string? TransactionId { get; set; }

        // Additional properties for better tracking
        public int NumberOfNights => (CheckOutDate - CheckInDate).Days;
        public string GuestEmail => User?.Email ?? string.Empty;
        public string GuestName => $"{User?.FirstName} {User?.LastName}";
    }
}