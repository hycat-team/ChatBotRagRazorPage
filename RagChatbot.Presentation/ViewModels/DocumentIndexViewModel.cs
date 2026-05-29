using RagChatbot.Business.DTOs;
using System.Collections.Generic;

namespace RagChatbot.Presentation.ViewModels
{
    public class DocumentIndexViewModel
    {
        public IEnumerable<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
    }
}
