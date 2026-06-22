using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ContactsModel : PageModel
    {
        private readonly RagChatbot.Business.Interfaces.IContactService _contactService;

        public ContactsModel(RagChatbot.Business.Interfaces.IContactService contactService)
        {
            _contactService = contactService;
        }

        // Khởi tạo sẵn một danh sách rỗng để phòng trường hợp DB chưa kịp tải dữ liệu
        public IEnumerable<RagChatbot.Business.DTOs.ContactMessageDto> ContactMessages { get; set; } = new List<RagChatbot.Business.DTOs.ContactMessageDto>();

        public async Task OnGetAsync()
        {
            ContactMessages = await _contactService.GetAllContactMessagesAsync();
        }
    }
}