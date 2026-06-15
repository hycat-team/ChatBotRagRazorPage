using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.EntityModels;
using System.Linq;
using System.Threading.Tasks;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ContactsModel : PageModel
    {
        private readonly RagChatbot.DataAccess.Data.ApplicationDbContext _context;

        public ContactsModel(RagChatbot.DataAccess.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public System.Collections.Generic.List<ContactMessage> ContactMessages { get; set; }

        public async Task OnGetAsync()
        {
            ContactMessages = await _context.ContactMessages
                                        .Include(c => c.User)
                                        .OrderByDescending(c => c.CreatedAt)
                                        .ToListAsync();
        }
    }
}
