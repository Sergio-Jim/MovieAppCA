using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MovieApp.Domain.Entities;
using MovieApp.Web.Models;
using System.Threading.Tasks;

namespace MovieApp.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<UserManagementController> _logger;

        public ProfileController(UserManager<User> userManager, SignInManager<User> signInManager, ILogger<UserManagementController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            return View(new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Update user details
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.UserName = model.UserName;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Handle email change if needed (send verification email)
                if (user.Email != model.Email)
                {
                    await _userManager.SetEmailAsync(user, model.Email);
                    await SendEmailVerificationEmail(user);
                    user.EmailConfirmed = false; // Require re-verification
                }

                // Update phone number
                if (user.PhoneNumber != model.PhoneNumber)
                {
                    user.PhoneNumber = model.PhoneNumber;
                    await _userManager.UpdateAsync(user);
                    await SendPhoneVerificationCode(user);
                    user.PhoneNumberConfirmed = false; // Require re-verification
                }

                // Update 2FA status
                if (model.TwoFactorEnabled != user.TwoFactorEnabled)
                {
                    user.TwoFactorEnabled = model.TwoFactorEnabled;
                    await _userManager.SetTwoFactorEnabledAsync(user, model.TwoFactorEnabled);
                }

                await _signInManager.RefreshSignInAsync(user); // Update sign-in cookie
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View("Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "Email verified successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Email verification failed.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> VerifyPhone(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, code);
            if (result.Succeeded)
            {
                user.PhoneNumberConfirmed = true;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "Phone number verified successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Phone verification failed.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> SendEmailVerification()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, errors = new[] { "User not found." } });
            }

            await SendEmailVerificationEmail(user);
            return Json(new { success = true, message = "Verification email sent. Check your inbox." });
        }

        [HttpPost]
        public async Task<IActionResult> SendPhoneVerification()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, errors = new[] { "User not found." } });
            }

            await SendPhoneVerificationCode(user);
            return Json(new { success = true, message = "Verification code sent to your phone." });
        }

        // requires config and implementation
        private async Task SendEmailVerificationEmail(User user)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action("VerifyEmail", "Profile", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
            // Implement email sending logic (e.g., using SendGrid, MailKit, or other email service)
            _logger.LogInformation("Sending email verification to {Email} with callback URL: {CallbackUrl}", user.Email, callbackUrl);
            // Example: Use an email service to send the verification link to user.Email
        }

        // requires config and implementation
        private async Task SendPhoneVerificationCode(User user)
        {
            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber);
            // Implement SMS sending logic (e.g., using Twilio, Nexmo, or other SMS service)
            _logger.LogInformation("Sending phone verification code {Code} to {PhoneNumber}", code, user.PhoneNumber);
            // Example: Use an SMS service to send the code to user.PhoneNumber
        }
    }
}