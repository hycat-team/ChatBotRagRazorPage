using RagChatbot.Business.DTOs;

namespace RagChatbot.PresentationRazorPage.ViewModels
{
    public class DocumentIndexViewModel
    {
        public IEnumerable<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
        public int? LastSelectedSubjectId { get; set; }
    }
}
