using Bookify.Data.Data;
using Bookify.Core.Models;
using Bookify.Data.Repositories;

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

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}