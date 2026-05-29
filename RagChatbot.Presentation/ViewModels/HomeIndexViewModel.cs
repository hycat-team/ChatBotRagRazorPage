using RagChatbot.Business.DTOs;
using System.Collections.Generic;

namespace RagChatbot.Presentation.ViewModels
{
    public class HomeIndexViewModel
    {
        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
    }
}
