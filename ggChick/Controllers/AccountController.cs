using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ggChick.Models;
using ggChick.ViewModels.Account;

namespace ggChick.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if username or email already exists
            var existingUser = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
            {
                ModelState.AddModelError("UserName", "This username is already taken. Please choose another.");
                return View(model);
            }

            var existingEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FullName = model.FullName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Default role is User
                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("User created a new account with password.");
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Allow login with either Username or Email
            var user = await _userManager.FindByNameAsync(model.UserNameOrEmail) 
                       ?? await _userManager.FindByEmailAsync(model.UserNameOrEmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, 
                model.Password, 
                model.RememberMe, 
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                // If user is Admin, redirect directly to Admin Dashboard
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login credentials. Please try again.");
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                ViewBag.Message = "If your email is registered, you will receive password reset instructions.";
                return View("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account", new { email = model.Email, token }, Request.Scheme);

            // In local/demo mode, display the direct reset link on screen for immediate testing
            ViewBag.ResetLink = resetLink;
            ViewBag.Token = token;
            ViewBag.Email = model.Email;
            return View("ForgotPasswordConfirmation");
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string? email, string? token)
        {
            if (email == null || token == null)
            {
                return RedirectToAction("Login");
            }

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your password has been successfully reset! You can now log in.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ========================================================
        // AJAX ENDPOINTS FOR REAL-TIME AVAILABILITY CHECKING
        // ========================================================

        // GET: /Account/CheckUsername?username=test
        [HttpGet]
        public async Task<IActionResult> CheckUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
            {
                return Json(new { available = false, message = "Username must be at least 3 characters." });
            }

            var clean = username.Trim();
            var exists = await _userManager.Users.AnyAsync(u => u.UserName!.ToLower() == clean.ToLower());

            return Json(new
            {
                available = !exists,
                message = !exists ? "✓ Username is available" : "✗ Username is already taken"
            });
        }

        // GET: /Account/CheckEmail?email=test@example.com
        [HttpGet]
        public async Task<IActionResult> CheckEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return Json(new { available = false, message = "Please enter a valid email address." });
            }

            var clean = email.Trim();
            var exists = await _userManager.Users.AnyAsync(u => u.Email!.ToLower() == clean.ToLower());

            return Json(new
            {
                available = !exists,
                message = !exists ? "✓ Email is available" : "✗ An account with this email already exists"
            });
        }
    }
}
