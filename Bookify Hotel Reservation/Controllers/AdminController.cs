using Bookify.Data;
using Bookify.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookifyHotelReservation.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IUnitOfWork unitOfWork, ILogger<AdminController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            var rooms = await _unitOfWork.Rooms.GetAllAsync();
            var bookings = await _unitOfWork.Bookings.GetAllAsync();

            var model = new AdminDashboardViewModel
            {
                TotalRooms = rooms.Count(),
                AvailableRooms = rooms.Count(r => r.IsAvailable),
                TotalBookings = bookings.Count(),
                RecentRooms = rooms.Take(5).ToList(),
                RecentBookings = bookings.Take(5).ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Rooms()
        {
            var rooms = await _unitOfWork.Rooms.GetAllAsync();
            return View(rooms);
        }

        public async Task<IActionResult> Bookings()
        {
            var bookings = await _unitOfWork.Bookings.GetAllAsync();
            return View(bookings);
        }
    }
}