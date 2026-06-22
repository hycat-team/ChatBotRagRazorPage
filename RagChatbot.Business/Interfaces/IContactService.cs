using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface IContactService
    {
        Task<IEnumerable<ContactMessageDto>> GetAllContactMessagesAsync();
        Task<int> GetPendingCountAsync();
        Task<ContactMessageDto?> GetByIdAsync(int id);
        Task AddContactMessageAsync(ContactMessageDto contactDto);
        Task UpdateContactStatusAsync(int id, string status);
        Task DeleteContactMessageAsync(int id);
    }
}
