using RagChatbot.Business.DTOs;
using System.Collections.Generic;

namespace RagChatbot.PresentationRazorPage.ViewModels
{
    public class HomeIndexViewModel
    {
        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
    }
}
