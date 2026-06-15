using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using RagChatbot.Business.Interfaces;

namespace RagChatbot.Business.Services
{
    public class EmailQueue : IEmailQueue
    {
        private readonly Channel<EmailMessage> _queue;

        public EmailQueue()
        {
            // Bounded channel to prevent out-of-memory if emails are generated faster than sent.
            // 1000 is a reasonable default for typical operations.
            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<EmailMessage>(options);
        }

        public async ValueTask QueueEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            await _queue.Writer.WriteAsync(message, cancellationToken);
        }

        public async ValueTask<EmailMessage> DequeueEmailAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
