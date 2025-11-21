using Bookify.Core.Models;
using Bookify.Data.Repositories;

namespace Bookify.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IRoomRepository Rooms { get; }
        IGenericRepository<RoomType> RoomTypes { get; }
        IGenericRepository<Booking> Bookings { get; }

        // Add specific booking methods
        Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId);
        Task<Booking?> GetBookingWithDetailsAsync(int id);

        Task<int> CompleteAsync();
    }
}