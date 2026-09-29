using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ggChick.Data;
using ggChick.Models;
using ggChick.ViewModels.Products;
using System.Security.Claims;

namespace ggChick.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: /Reviews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReviewInputModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide both a valid star rating (1-5) and a review message.";
                return RedirectToAction("Details", "Products", new { id = model.ProductId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Check if user already reviewed this product -> update it
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == model.ProductId && r.UserId == userId);

            if (existingReview != null)
            {
                existingReview.Rating = model.Rating;
                existingReview.Comment = model.Comment;
                existingReview.UpdatedAt = DateTime.UtcNow;
                _context.Update(existingReview);
            }
            else
            {
                var review = new Review
                {
                    ProductId = model.ProductId,
                    UserId = userId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Reviews.Add(review);
            }

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Thank you! Your review has been submitted." });
            }

            TempData["SuccessMessage"] = "Thank you! Your review has been saved.";
            return RedirectToAction("Details", "Products", new { id = model.ProductId });
        }

        // POST: /Reviews/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var review = await _context.Reviews.FindAsync(id);

            if (review == null)
            {
                return NotFound();
            }

            // Only author or Admin can delete
            if (review.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var productId = review.ProductId;
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Review removed.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }
    }
}
