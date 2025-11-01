namespace Services.Interfaces
{
    public interface IEmailBody
    {
        string GenerateRegisterConfirmEmailBody(string userName, string confirmationLink, string token);
    }
}