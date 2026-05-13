using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextCart.Data;
using NextCart.Models;

namespace NextCart.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CheckoutController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // CHECKOUT PAGE
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            ViewBag.Total = cartItems.Sum(c =>
                (decimal)c.Product.Price * c.Quantity
            );

            return View(cartItems);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder()
        {
            Console.WriteLine("PLACE ORDER METHOD HIT");

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            foreach (var item in cartItems)
            {
                Order order = new Order()
                {
                    UserId = user.Id,
                    ProductName = item.Product.Name,

                    // FIXED: double → decimal conversion
                    Price = (decimal)item.Product.Price,
                    Quantity = item.Quantity,

                    TotalPrice = (decimal)(item.Product.Price * item.Quantity),

                    OrderDate = DateTime.Now,
                    Status = "Pending"
                };

                _context.Orders.Add(order);
            }

            // CLEAR CART
            _context.CartItems.RemoveRange(cartItems);

            // SAVE ALL CHANGES
            await _context.SaveChangesAsync();

            return RedirectToAction("Success");
        }

        // SUCCESS PAGE
        public IActionResult Success()
        {
            return View();
        }
    }
}