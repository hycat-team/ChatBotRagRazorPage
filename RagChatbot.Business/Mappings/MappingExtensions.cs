using RagChatbot.Business.DTOs;
using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Mappings
{
    public static class MappingExtensions
    {
        // Subject Mappings
        public static SubjectDto? ToDto(this Subject? entity, bool includeRelations = true)
        {
            if (entity == null) return null;
            var dto = new SubjectDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                CreatedAt = entity.CreatedAt,
                IsActive = entity.IsActive,
                ManagerName = entity.Department?.Users?.FirstOrDefault(u => u.Role == "HeadOfDepartment") != null
                    ? $"{entity.Department.Users.First(u => u.Role == "HeadOfDepartment").LastName} {entity.Department.Users.First(u => u.Role == "HeadOfDepartment").FirstName}".Trim()
                    : "Trống",
                DepartmentId = entity.DepartmentId,
                DepartmentName = entity.Department?.Name ?? string.Empty
            };
            if (includeRelations && entity.Documents != null)
            {
                dto.Documents = entity.Documents.Select(d => d.ToDto(false)!).ToList();
            }
            return dto;
        }

        public static Subject ToEntity(this CreateSubjectDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new Subject
            {
                Code = dto.Code,
                Name = dto.Name,
                IsActive = true,
                DepartmentId = dto.DepartmentId,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Document Mappings
        public static DocumentDto? ToDto(this Document? entity, bool includeRelations = true)
        {
            if (entity == null) return null;
            var dto = new DocumentDto
            {
                Id = entity.Id,
                SubjectId = entity.SubjectId,
                FileName = entity.FileName,
                FilePath = entity.FilePath,
                DisplayName = entity.DisplayName,
                IsActive = entity.IsActive,
                Status = entity.Status,
                UploadedAt = entity.UploadedAt,
                UploaderFullName = entity.Uploader != null ? $"{entity.Uploader.LastName} {entity.Uploader.FirstName}".Trim() : "Không xác định",
                UploaderId = entity.UploaderId
            };
            if (includeRelations)
            {
                if (entity.Subject != null) dto.Subject = entity.Subject.ToDto(false);
                if (entity.DocumentChunks != null) dto.DocumentChunks = entity.DocumentChunks.Select(c => c.ToDto(false)!).ToList();
            }
            return dto;
        }

        public static Document ToEntity(this CreateDocumentDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new Document
            {
                SubjectId = dto.SubjectId,
                FileName = dto.FileName,
                FilePath = dto.FilePath,
                DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.FileName : dto.DisplayName,
                IsActive = dto.IsActive,
                Status = dto.Status,
                UploadedAt = dto.UploadedAt,
                UploaderId = dto.UploaderId
            };
        }

        // DocumentChunk Mappings
        public static DocumentChunkDto? ToDto(this DocumentChunk? entity, bool includeRelations = true)
        {
            if (entity == null) return null;
            var dto = new DocumentChunkDto
            {
                Id = entity.Id,
                DocumentId = entity.DocumentId,
                Content = entity.Content,
                PageNumber = entity.PageNumber
            };
            if (includeRelations && entity.Document != null)
            {
                dto.Document = entity.Document.ToDto(false);
            }
            return dto;
        }

        // ChatSession Mappings
        public static ChatSessionDto? ToDto(this ChatSession? entity)
        {
            if (entity == null) return null;
            return new ChatSessionDto
            {
                Id = entity.Id,
                SubjectId = entity.SubjectId,
                Title = entity.Title,
                CreatedAt = entity.CreatedAt,
                UserId = entity.UserId
            };
        }

        public static ChatSession ToEntity(this CreateChatSessionDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new ChatSession
            {
                SubjectId = dto.SubjectId,
                Title = dto.Title,
                CreatedAt = DateTime.UtcNow,
                UserId = dto.UserId
            };
        }

        // ChatMessage Mappings
        public static ChatMessageDto? ToDto(this ChatMessage? entity)
        {
            if (entity == null) return null;
            return new ChatMessageDto
            {
                Id = entity.Id,
                SessionId = entity.SessionId,
                Role = entity.Role,
                Content = entity.Content,
                Citations = entity.Citations,
                Timestamp = entity.Timestamp
            };
        }

        public static ChatMessage ToEntity(this CreateChatMessageDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new ChatMessage
            {
                SessionId = dto.SessionId,
                Role = dto.Role,
                Content = dto.Content,
                Citations = dto.Citations,
                Timestamp = dto.Timestamp
            };
        }
    }
}
