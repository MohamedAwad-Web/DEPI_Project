using Bookify.Core.Models;
using Bookify.Data;
using Bookify.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace BookifyHotelReservation.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PaymentService _paymentService;
        private readonly ReservationCartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<BookingController> _logger;
        private readonly IConfiguration _configuration;

        public BookingController(IUnitOfWork unitOfWork, PaymentService paymentService,
                               ReservationCartService cartService, UserManager<ApplicationUser> userManager,
                               ILogger<BookingController> logger, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _cartService = cartService;
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var cart = _cartService.GetCart();
                if (cart == null)
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Cart", "Home");
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Login", "Account");
                }

                var room = await _unitOfWork.Rooms.GetRoomWithTypeAsync(cart.RoomId);
                if (room == null || !room.IsAvailable)
                {
                    TempData["Error"] = "Sorry, this room is no longer available.";
                    _cartService.ClearCart();
                    return RedirectToAction("Index", "Home");
                }

                // Create pending booking record
                var booking = new Booking
                {
                    UserId = user.Id,
                    RoomId = cart.RoomId,
                    CheckInDate = cart.CheckInDate,
                    CheckOutDate = cart.CheckOutDate,
                    TotalCost = cart.TotalPrice,
                    Status = "Pending",
                    BookingDate = DateTime.UtcNow
                };

                await _unitOfWork.Bookings.AddAsync(booking);
                await _unitOfWork.CompleteAsync();

                // Create Stripe checkout session
                var bookingDescription = $"Room {room.RoomNumber} - {room.RoomType.Name} - {cart.CheckInDate:MMM dd} to {cart.CheckOutDate:MMM dd, yyyy}";

                // Ensure URLs are not null
                var successUrl = Url.Action("PaymentSuccess", "Booking", new { bookingId = booking.Id }, Request.Scheme)
                                ?? $"{Request.Scheme}://{Request.Host}/Booking/PaymentSuccess?bookingId={booking.Id}";

                var cancelUrl = Url.Action("PaymentCancelled", "Booking", new { bookingId = booking.Id }, Request.Scheme)
                               ?? $"{Request.Scheme}://{Request.Host}/Booking/PaymentCancelled?bookingId={booking.Id}";

                var metadata = new Dictionary<string, string>
                {
                    { "booking_id", booking.Id.ToString() },
                    { "room_number", room.RoomNumber },
                    { "customer_email", user.Email ?? string.Empty } // Handle null email
                };

                var session = _paymentService.CreateCheckoutSession(
                    cart.TotalPrice,
                    bookingDescription,
                    successUrl,
                    cancelUrl,
                    metadata
                );

                // Store session ID in booking for verification
                booking.StripePaymentIntentId = session.Id;
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Created Stripe checkout session {SessionId} for booking {BookingId}", session.Id, booking.Id);

                return Redirect(session.Url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout");
                TempData["Error"] = $"An error occurred during checkout: {ex.Message}";
                return RedirectToAction("Cart", "Home");
            }
        }

        public async Task<IActionResult> PaymentSuccess(int bookingId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Login", "Account");
                }

                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null || booking.UserId != userId)
                {
                    TempData["Error"] = "Booking not found.";
                    return RedirectToAction("Index", "Home");
                }

                if (string.IsNullOrEmpty(booking.StripePaymentIntentId))
                {
                    TempData["Error"] = "Payment session not found.";
                    return RedirectToAction("PaymentCancelled", new { bookingId });
                }

                // Verify payment with Stripe
                var paymentSuccessful = _paymentService.IsPaymentSuccessful(booking.StripePaymentIntentId);

                if (paymentSuccessful)
                {
                    // Get the Stripe session for additional details
                    var stripeSession = _paymentService.GetCheckoutSession(booking.StripePaymentIntentId);

                    booking.Status = "Confirmed";
                    booking.TransactionId = stripeSession.PaymentIntentId ?? "Unknown";

                    // Mark room as unavailable
                    var room = await _unitOfWork.Rooms.GetByIdAsync(booking.RoomId);
                    if (room != null)
                    {
                        room.IsAvailable = false;
                    }

                    await _unitOfWork.CompleteAsync();
                    _cartService.ClearCart();

                    TempData["Success"] = $"Payment successful! Your booking for Room {room?.RoomNumber} is confirmed.";
                    _logger.LogInformation("Booking {BookingId} confirmed with payment {PaymentIntent}", booking.Id, stripeSession.PaymentIntentId);

                    return View(booking);
                }
                else
                {
                    TempData["Error"] = "Payment verification failed. Please contact support.";
                    return RedirectToAction("PaymentCancelled", new { bookingId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PaymentSuccess for booking {BookingId}", bookingId);
                TempData["Error"] = "An error occurred while processing your payment.";
                return RedirectToAction("PaymentCancelled", new { bookingId });
            }
        }

        public async Task<IActionResult> PaymentCancelled(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking != null && booking.UserId == userId && booking.Status == "Pending")
                {
                    // Optionally: Delete the pending booking or keep it for retry
                    // For now, we'll keep it but mark as cancelled
                    booking.Status = "Cancelled";
                    await _unitOfWork.CompleteAsync();
                }
            }

            _cartService.ClearCart();
            TempData["Error"] = "Payment was cancelled. Your booking has not been confirmed.";

            return View();
        }

        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _unitOfWork.GetUserBookingsAsync(userId);
            return View(bookings);
        }

        public async Task<IActionResult> BookingDetails(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            var booking = await _unitOfWork.GetBookingWithDetailsAsync(id);
            if (booking == null || booking.UserId != userId)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var booking = await _unitOfWork.Bookings.GetByIdAsync(id);

                if (booking == null || booking.UserId != userId)
                {
                    TempData["Error"] = "Booking not found.";
                    return RedirectToAction("MyBookings");
                }

                if (booking.Status != "Confirmed")
                {
                    TempData["Error"] = "Only confirmed bookings can be cancelled.";
                    return RedirectToAction("MyBookings");
                }

                if (booking.CheckInDate <= DateTime.Today)
                {
                    TempData["Error"] = "Cannot cancel booking that has already started or passed.";
                    return RedirectToAction("MyBookings");
                }

                booking.Status = "Cancelled";

                // Make room available again
                var room = await _unitOfWork.Rooms.GetByIdAsync(booking.RoomId);
                if (room != null)
                {
                    room.IsAvailable = true;
                }

                await _unitOfWork.CompleteAsync();

                TempData["Success"] = "Booking cancelled successfully.";
                _logger.LogInformation("Booking {BookingId} cancelled by user {UserId}", id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
                TempData["Error"] = "An error occurred while cancelling the booking.";
            }

            return RedirectToAction("MyBookings");
        }
    }
}