using Bookify.Data;
using Bookify.Data.Entities;
using Stripe;
using Microsoft.Extensions.Configuration;

namespace Bookify.Services;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;

    public BookingService(IUnitOfWork uow, IConfiguration config)
    {
        _uow = uow;
        _config = config;
        var apiKey = _config["Stripe:SecretKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            StripeConfiguration.ApiKey = apiKey;
        }
    }

    public async Task<(Booking booking, string clientSecret)> CreateBookingWithPaymentAsync(string userId, int roomId, DateTime checkIn, DateTime checkOut, string currency, string? promoCode = null)
    {
        var room = await _uow.Rooms.GetByIdAsync(roomId);
        if (room is null) throw new InvalidOperationException("Room not found");
        if (!room.IsAvailable) throw new InvalidOperationException("Room not available");

        var days = Math.Max(1, (checkOut.Date - checkIn.Date).Days);
        var roomType = (await _uow.Repository<RoomType>().GetAllAsync(r => r.Id == room.RoomTypeId)).First();
        var total = roomType.BasePricePerNight * days;
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            // Simple placeholder: 10% off for any non-empty promo
            total = Math.Round(total * 0.9m, 2);
        }

        using (await _uow.BeginTransactionAsync())
        {
            var paymentIntentService = new PaymentIntentService();
            var intent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = (long)(total * 100),
                Currency = currency,
                Metadata = new Dictionary<string, string>
                {
                    ["roomId"] = roomId.ToString(),
                    ["userId"] = userId,
                }
            });

            var booking = new Booking
            {
                UserId = userId,
                RoomId = roomId,
                CheckIn = checkIn,
                CheckOut = checkOut,
                TotalCost = total,
                StripePaymentIntentId = intent.Id,
                Status = "Pending"
            };
            await _uow.Bookings.AddAsync(booking);

            await _uow.SaveChangesAsync();

            return (booking, intent.ClientSecret);
        }
    }

    public Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId)
    {
        return _uow.Bookings.GetUserBookingsAsync(userId);
    }

    public async Task<bool> MarkPaidAsync(string paymentIntentId, string userId)
    {
        var booking = (await _uow.Bookings.GetUserBookingsAsync(userId)).FirstOrDefault(b => b.StripePaymentIntentId == paymentIntentId);
        if (booking == null) return false;
        booking.Status = "Paid";
        _uow.Bookings.Update(booking);
        await _uow.SaveChangesAsync();
        return true;
    }
}