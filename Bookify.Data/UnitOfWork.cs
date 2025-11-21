using Bookify.Core.Models;
using Bookify.Data.Data;
using Bookify.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Rooms = new RoomRepository(_context);
            RoomTypes = new GenericRepository<RoomType>(_context);
            Bookings = new GenericRepository<Booking>(_context);
        }

        public IRoomRepository Rooms { get; private set; }
        public IGenericRepository<RoomType> RoomTypes { get; private set; }
        public IGenericRepository<Booking> Bookings { get; private set; }

        public async Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId)
        {
            return await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingWithDetailsAsync(int id)
        {
            return await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}