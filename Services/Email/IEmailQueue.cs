namespace Services.Email
{
    public interface IEmailQueue
    {
        void QueueEmail(string to, string subject, string body);
        ValueTask<EmailMessage> DequeueAsync(CancellationToken cancellationToken);
    }
}
