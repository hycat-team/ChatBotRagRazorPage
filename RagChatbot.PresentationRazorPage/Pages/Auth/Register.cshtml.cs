using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RagChatbot.PresentationRazorPage.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        public IActionResult OnGet()
        {
            return RedirectToPage("/Auth/Login");
        }
    }
}
