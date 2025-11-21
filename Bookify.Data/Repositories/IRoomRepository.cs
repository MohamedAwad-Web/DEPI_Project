using Bookify.Core.Models;

namespace Bookify.Data.Repositories
{
    public interface IRoomRepository : IGenericRepository<Room>
    {
        Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut);
        Task<Room> GetRoomWithTypeAsync(int id);
    }
}