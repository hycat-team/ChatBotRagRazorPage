using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Services
{
    public class ContactService : IContactService
    {
        private readonly ApplicationDbContext _context;

        public ContactService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ContactMessageDto>> GetAllContactMessagesAsync()
        {
            var messages = await _context.ContactMessages
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return messages.Select(MapToDto);
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.ContactMessages.CountAsync(c => c.Status == ContactStatus.Pending);
        }

        public async Task<ContactMessageDto?> GetByIdAsync(int id)
        {
            var message = await _context.ContactMessages
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);
            return message == null ? null : MapToDto(message);
        }

        public async Task AddContactMessageAsync(ContactMessageDto contactDto)
        {
            var message = new ContactMessage
            {
                UserId = contactDto.UserId,
                Content = contactDto.Content,
                Type = System.Enum.Parse<ContactType>(contactDto.Type),
                Status = ContactStatus.Pending,
                RelatedId = contactDto.RelatedId,
                CreatedAt = System.DateTime.UtcNow
            };
            _context.ContactMessages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateContactStatusAsync(int id, string status)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                message.Status = System.Enum.Parse<ContactStatus>(status);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteContactMessageAsync(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                _context.ContactMessages.Remove(message);
                await _context.SaveChangesAsync();
            }
        }

        private ContactMessageDto MapToDto(ContactMessage msg)
        {
            return new ContactMessageDto
            {
                Id = msg.Id,
                UserId = msg.UserId,
                Content = msg.Content,
                Type = msg.Type.ToString(),
                Status = msg.Status.ToString(),
                RelatedId = msg.RelatedId,
                CreatedAt = msg.CreatedAt,
                User = msg.User == null ? null : new AppUserDto
                {
                    Id = msg.User.Id,
                    Email = msg.User.Email,
                    FirstName = msg.User.FirstName,
                    LastName = msg.User.LastName
                }
            };
        }
    }
}
