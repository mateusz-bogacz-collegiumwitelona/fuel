namespace Services.Interfaces
{
    public interface IEmaliBody
    {
        string GenerateConfirmEmailBody(string userName, string confirmationLink, string token);
    }
}
