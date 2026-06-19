using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.EntityModels;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RagChatbot.PresentationRazorPage.Pages.Document
{
    [Authorize(Roles = "Student")]
    public class BrowseModel : PageModel
    {
        private readonly RagChatbot.DataAccess.Data.ApplicationDbContext _context;

        public BrowseModel(RagChatbot.DataAccess.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public System.Collections.Generic.List<RagChatbot.DataAccess.EntityModels.Document> Documents { get; set; } = new List<RagChatbot.DataAccess.EntityModels.Document>();

        public async Task OnGetAsync(int? subjectId, string searchString)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isPremium = false;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _context.AppUsers.FindAsync(userId);
                if (user != null)
                {
                    isPremium = user.Subscription == AppUser.SubscriptionType.Premium;
                }
            }
            ViewData["IsPremium"] = isPremium; 
                                           
            var subjects = _context.Subjects.OrderBy(s => s.Name).ToList();

            var query = _context.Documents
                .Include(d => d.Subject)
                .Where(d => d.Status == "Indexed" && d.IsActive)
                .AsQueryable();

            if (subjectId.HasValue && subjectId.Value > 0)
            {
                query = query.Where(d => d.SubjectId == subjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string keyword = searchString.Trim().ToLower();
                query = query.Where(d => d.FileName.ToLower().Contains(keyword) ||
                                     (d.DisplayName != null && d.DisplayName.ToLower().Contains(keyword)));
            }

            Documents = await query.OrderByDescending(d => d.UploadedAt).ToListAsync();

            ViewData["Subjects"] = subjects;
            ViewData["SelectedSubjectId"] = subjectId;
            ViewData["SearchString"] = searchString;
        }
    }
}
