using System;
using System.Collections.Generic;

namespace RagChatbot.Business.DTOs
{
    public class SubjectDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public ICollection<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
    }

    public class CreateSubjectDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int? DepartmentId { get; set; }
    }
}
