using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Services.Helpers
{
    public class GoogleAuthClient
    {
        private readonly string _clientId;

        public GoogleAuthClient(IOptions<GoogleOptions> options)
        {
            _clientId = options.Value.ClientId;
        }

        public async Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken)
        {
            try
            {
                return await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _clientId }
                    });
            }
            catch
            {
                return null;
            }
        }



    }
}
