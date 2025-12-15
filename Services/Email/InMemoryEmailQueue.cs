using System.Threading.Channels;

namespace Services.Email
{
    public class InMemoryEmailQueue : IEmailQueue 
    {
        private readonly Channel<(string To, string Subject, string Body)> _queue;

        public InMemoryEmailQueue()
        {
            var option = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };

            _queue = Channel.CreateBounded<(string, string, string)>(option);
        }

        public void QueueEmail(string to, string subject, string body)
            => _queue.Writer.TryWrite((to, subject, body));

        public async ValueTask<EmailMessage> DequeueAsync(CancellationToken cancellationToken)
        {
            var (to, subject, body) = await _queue.Reader.ReadAsync(cancellationToken);
            return new EmailMessage(to, subject, body);
        }
    }
}
