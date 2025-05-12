using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuizGen.BLL.Models.Quiz;
using QuizGen.BLL.Services.Interfaces;
using System.Text.Json;

namespace QuizGen.API.Pages
{
    public class QuizListModel : PageModel
    {
        private readonly IQuizService _quizService;
        private const int PageSize = 9;

        public QuizListModel(IQuizService quizService)
        {
            _quizService = quizService;
            ErrorMessage = string.Empty;
        }

        public IEnumerable<QuizDto> Quizzes { get; private set; } = Enumerable.Empty<QuizDto>();
        public string ErrorMessage { get; private set; }
        
        // For debugging purposes
        public string DebugInfo { get; private set; } = string.Empty;

        [BindProperty]
        public int QuizId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        public int TotalPages { get; private set; }
        public int TotalItems { get; private set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        
        // Property for PageSize to be used in the view
        public int GetPageSize => PageSize;

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is authenticated
            var authToken = Request.Cookies["AuthToken"];
            var userId = Request.Cookies["UserId"];

            if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                // Parse the user ID
                if (!int.TryParse(userId, out int currentUserId))
                {
                    ErrorMessage = "Invalid user ID";
                    return Page();
                }

                // Store debug info
                DebugInfo = $"User ID: {currentUserId}, Auth Token exists: {!string.IsNullOrEmpty(authToken)}";

                // Use the quiz service to get the quizzes
                var result = await _quizService.GetQuizzesByAuthorAsync(currentUserId);
                
                if (result.Success)
                {
                    var allQuizzes = result.Data.ToList();
                    
                    // Sort quizzes by creation date (newest first)
                    allQuizzes = allQuizzes.OrderByDescending(q => q.CreatedAt).ToList();
                    
                    // Apply search filter if provided
                    if (!string.IsNullOrWhiteSpace(SearchTerm))
                    {
                        allQuizzes = allQuizzes
                            .Where(q => q.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                    
                    TotalItems = allQuizzes.Count;
                    TotalPages = TotalItems > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 1;
                    
                    // Ensure current page is within valid range
                    if (CurrentPage < 1)
                        CurrentPage = 1;
                    else if (CurrentPage > TotalPages && TotalPages > 0)
                        CurrentPage = TotalPages;
                    
                    // Apply pagination
                    Quizzes = allQuizzes
                        .Skip((CurrentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToList();
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                // Include inner exception details for more debugging information
                ErrorMessage = $"An error occurred while retrieving quizzes: {ex.Message}";
                if (ex.InnerException != null)
                {
                    ErrorMessage += $" Inner exception: {ex.InnerException.Message}";
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Check if user is authenticated
            var authToken = Request.Cookies["AuthToken"];
            var userId = Request.Cookies["UserId"];

            if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Login");
            }

            if (QuizId <= 0)
            {
                return BadRequest("Invalid quiz ID");
            }

            try
            {
                // Use the quiz service to delete the quiz
                var result = await _quizService.DeleteQuizAsync(QuizId);
                
                if (!result.Success)
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                // Include inner exception details for more debugging information
                ErrorMessage = $"An error occurred while deleting the quiz: {ex.Message}";
                if (ex.InnerException != null)
                {
                    ErrorMessage += $" Inner exception: {ex.InnerException.Message}";
                }
            }

            // Redirect to refresh the page
            return RedirectToPage();
        }
    }
} 