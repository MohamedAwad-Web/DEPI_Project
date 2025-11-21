using Bookify.Core.Models;
using Bookify.Data;
using Bookify.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookifyHotelReservation.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HomeController> _logger;
        private readonly ReservationCartService _cartService;

        public HomeController(IUnitOfWork unitOfWork, ILogger<HomeController> logger,
                            ReservationCartService cartService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cartService = cartService;
        }


        public async Task<IActionResult> Index()
        {
            var rooms = await _unitOfWork.Rooms.GetAllAsync();
            return View(rooms);
        }

        public async Task<IActionResult> Details(int id)
        {
            var room = await _unitOfWork.Rooms.GetRoomWithTypeAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            // Set default dates (tomorrow for check-in, day after for check-out)
            var model = new RoomDetailsViewModel
            {
                Room = room,
                CheckInDate = DateTime.Today.AddDays(1),
                CheckOutDate = DateTime.Today.AddDays(2)
            };

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> AddToCart(int roomId, DateTime checkInDate, DateTime checkOutDate)
        {
            var room = await _unitOfWork.Rooms.GetRoomWithTypeAsync(roomId);
            if (room == null)
            {
                return NotFound();
            }

            // Basic validation
            if (checkInDate >= checkOutDate)
            {
                TempData["Error"] = "Check-out date must be after check-in date.";
                return RedirectToAction("Details", new { id = roomId });
            }

            if (checkInDate < DateTime.Today)
            {
                TempData["Error"] = "Check-in date cannot be in the past.";
                return RedirectToAction("Details", new { id = roomId });
            }

            // Check room availability
            var availableRooms = await _unitOfWork.Rooms.GetAvailableRoomsAsync(checkInDate, checkOutDate);
            if (!availableRooms.Any(r => r.Id == roomId))
            {
                TempData["Error"] = "Sorry, this room is not available for the selected dates.";
                return RedirectToAction("Details", new { id = roomId });
            }

            // Create cart item
            var cart = new ReservationCart
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                RoomType = room.RoomType.Name,
                PricePerNight = room.RoomType.Price,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate
            };

            _cartService.AddToCart(cart);
            TempData["Success"] = "Room added to cart successfully!";

            return RedirectToAction("Cart");
        }

        public IActionResult Cart()
        {
            var cart = _cartService.GetCart();
            return View(cart);
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            _cartService.ClearCart();
            TempData["Success"] = "Cart cleared successfully!";
            return RedirectToAction("Index");
        }
    }
}