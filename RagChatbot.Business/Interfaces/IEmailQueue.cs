namespace RagChatbot.Business.Interfaces
{
    public record EmailMessage(string ToEmail, string Subject, string Body);

    public interface IEmailQueue
    {
        ValueTask QueueEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
        ValueTask<EmailMessage> DequeueEmailAsync(CancellationToken cancellationToken);
    }
}
