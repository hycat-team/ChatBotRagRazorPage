using RagChatbot.Business.DTOs;

namespace RagChatbot.PresentationRazorPage.ViewModels
{
    public class HomeIndexViewModel
    {
        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
    }
}
