using Data.Models;
using System.IdentityModel.Tokens.Jwt;

namespace Services.Interfaces
{
    public interface ITokenFactory
    {
        JwtSecurityToken CreateJwtToken(ApplicationUser user, IList<string> roles);
        RefreshToken CreateRefreshToken(Guid userId, string? ip, string? userAgent);
    }
}
