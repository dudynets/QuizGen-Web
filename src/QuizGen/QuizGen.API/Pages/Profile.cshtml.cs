using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuizGen.BLL.Models.Auth;
using QuizGen.BLL.Services.Interfaces;
using System.Net.Http.Headers;
using Microsoft.Extensions.Primitives;

namespace QuizGen.API.Pages
{
    public class ProfileModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProfileModel(IAuthService authService, IHttpContextAccessor httpContextAccessor)
        {
            _authService = authService;
            _httpContextAccessor = httpContextAccessor;
            StatusMessage = string.Empty;
        }

        [BindProperty]
        public UpdateProfileRequest ProfileRequest { get; set; } = new UpdateProfileRequest();

        public string StatusMessage { get; set; }
        public bool IsSuccess { get; set; }

        private int GetCurrentUserId()
        {
            var userId = Request.Cookies["UserId"];
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return 0;
            }
            return currentUserId;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is authenticated
            var authToken = Request.Cookies["AuthToken"];
            var userId = Request.Cookies["UserId"];

            if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Login");
            }

            if (!int.TryParse(userId, out int currentUserId))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                // Set authentication header for the request
                if (_httpContextAccessor.HttpContext != null)
                {
                    _httpContextAccessor.HttpContext.Request.Headers["Authorization"] = 
                        new StringValues($"Bearer {authToken}");
                }

                // Get current user details
                var result = await _authService.GetCurrentUserAsync(currentUserId);
                
                if (result.Success)
                {
                    // Populate the form with current values
                    ProfileRequest.Name = result.Data.Name;
                    ProfileRequest.OpenAiApiKey = result.Data.OpenAiApiKey;
                    ProfileRequest.GptModel = result.Data.GptModel;
                    
                    return Page();
                }
                else
                {
                    StatusMessage = "Error loading profile. Please try again.";
                    IsSuccess = false;
                    return Page();
                }
            }
            catch (Exception)
            {
                StatusMessage = "An error occurred while loading your profile.";
                IsSuccess = false;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (string.IsNullOrWhiteSpace(ProfileRequest.OpenAiApiKey))
            {
                ModelState.AddModelError("ProfileRequest.OpenAiApiKey", "OpenAI API key is required for quiz generation");
                return Page();
            }

            var result = await _authService.UpdateProfileAsync(
                GetCurrentUserId(),
                ProfileRequest.Name,
                ProfileRequest.OpenAiApiKey,
                ProfileRequest.GptModel
            );

            if (!result.Success)
            {
                StatusMessage = result.Message;
                IsSuccess = false;
                return Page();
            }

            StatusMessage = "Profile updated successfully";
            IsSuccess = true;
            return Page();
        }
    }
} 