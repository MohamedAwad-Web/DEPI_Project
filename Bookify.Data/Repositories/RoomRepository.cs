using Bookify.Core.Models;
using Bookify.Data.Data;  // Updated reference
using Microsoft.EntityFrameworkCore;

namespace Bookify.Data.Repositories
{
    public class RoomRepository : GenericRepository<Room>, IRoomRepository
    {
        public RoomRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut)
        {
            return await _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.IsAvailable &&
                       !r.Bookings.Any(b =>
                           (checkIn >= b.CheckInDate && checkIn < b.CheckOutDate) ||
                           (checkOut > b.CheckInDate && checkOut <= b.CheckOutDate) ||
                           (checkIn <= b.CheckInDate && checkOut >= b.CheckOutDate)))
                .ToListAsync();
        }

        public async Task<Room> GetRoomWithTypeAsync(int id)
        {
            return await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
    }
}