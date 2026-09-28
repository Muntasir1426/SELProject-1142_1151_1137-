using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ggChick.Models;

namespace ggChick.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Include(u => u.Orders)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var userRoleViewModels = new List<UserRowViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoleViewModels.Add(new UserRowViewModel
                {
                    User = user,
                    Roles = roles.ToList()
                });
            }

            return View(userRoleViewModels);
        }
    }

    public class UserRowViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public List<string> Roles { get; set; } = new List<string>();
    }
}
