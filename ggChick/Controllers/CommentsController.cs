using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ggChick.Data;
using ggChick.Models;
using ggChick.ViewModels.Products;
using System.Security.Claims;

namespace ggChick.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CommentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: /Comments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommentInputModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Comment cannot be empty.";
                return RedirectToAction("Details", "Products", new { id = model.ProductId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var comment = new Comment
            {
                ProductId = model.ProductId,
                UserId = userId,
                Content = model.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Comment posted successfully!" });
            }

            TempData["SuccessMessage"] = "Comment posted!";
            return RedirectToAction("Details", "Products", new { id = model.ProductId });
        }

        // POST: /Comments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var comment = await _context.Comments.FindAsync(id);

            if (comment == null)
            {
                return NotFound();
            }

            if (comment.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var productId = comment.ProductId;
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment deleted.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }
    }
}
