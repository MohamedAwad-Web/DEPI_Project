using Bookify.Core.Models;
using Bookify.Data.Repositories;

namespace Bookify.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IRoomRepository Rooms { get; }
        IGenericRepository<RoomType> RoomTypes { get; }
        IGenericRepository<Booking> Bookings { get; }
        Task<int> CompleteAsync();
    }
}