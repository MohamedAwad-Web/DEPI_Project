using Bookify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Security.Claims;

namespace Bookify.Web.Controllers;

public class BookingController : Controller
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [Authorize]
    [HttpPost]
    public IActionResult AddToCart(int roomId, DateTime checkIn, DateTime checkOut)
    {
        var item = new ReservationCartItem { RoomId = roomId, CheckIn = checkIn, CheckOut = checkOut };
        var cart = HttpContext.Session.GetString("BookingCart");
        var items = string.IsNullOrEmpty(cart) ? new List<ReservationCartItem>() : JsonSerializer.Deserialize<List<ReservationCartItem>>(cart)!;
        items.Add(item);
        HttpContext.Session.SetString("BookingCart", JsonSerializer.Serialize(items));
        TempData["toast"] = "Room added to reservation cart";
        return RedirectToAction("Cart");
    }

    [Authorize]
    public IActionResult Cart()
    {
        var cart = HttpContext.Session.GetString("BookingCart");
        var items = string.IsNullOrEmpty(cart) ? new List<ReservationCartItem>() : JsonSerializer.Deserialize<List<ReservationCartItem>>(cart)!;
        return View(items);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Checkout(int roomId, DateTime checkIn, DateTime checkOut, string currency = "usd", string? promoCode = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (booking, clientSecret) = await _bookingService.CreateBookingWithPaymentAsync(userId!, roomId, checkIn, checkOut, currency, promoCode);
        ViewBag.ClientSecret = clientSecret;
        return View(booking);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Confirm([FromBody] PaymentConfirmRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var ok = await _bookingService.MarkPaidAsync(request.PaymentIntentId, userId);
        if (!ok) return NotFound();
        return Ok();
    }

    [Authorize]
    public async Task<IActionResult> History()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var bookings = await _bookingService.GetUserBookingsAsync(userId!);
        return View(bookings);
    }
}

public class ReservationCartItem
{
    public int RoomId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
}

public record PaymentConfirmRequest(string PaymentIntentId);