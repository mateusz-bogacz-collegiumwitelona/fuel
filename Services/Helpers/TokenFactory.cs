using Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Services.Helpers
{
    public class TokenFactory : ITokenFactory
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TokenFactory> _logger;

        public TokenFactory(IConfiguration config, ILogger<TokenFactory> logger)
        {
            _config = config;
            _logger = logger;
        }

        public JwtSecurityToken CreateJwtToken(ApplicationUser user, IList<string> roles)
        {
            try
            {
                if (user == null)
                {
                    _logger.LogError("User is null when creating JWT token.");
                    throw new ArgumentNullException(nameof(user), "User cannot be null.");
                }

                if (roles == null || !roles.Any())
                {
                    _logger.LogError("Roles list is null when creating JWT token for user {UserId}.", user.Id);
                    throw new ArgumentNullException(nameof(roles), "Roles cannot be null.");
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                foreach (var role in roles)
                    claims.Add(new Claim(ClaimTypes.Role, role));

                var keyString = _config["Jwt:Key"];
                var issuer = _config["Jwt:Issuer"];
                var audience = _config["Jwt:Audience"];

                if (string.IsNullOrEmpty(keyString))
                {
                    _logger.LogError("JWT Key is not configured properly.");
                    throw new InvalidOperationException("JWT Key is missing in configuration.");
                }

                if (keyString.Length < 32)
                {
                    _logger.LogError("JWT Key is too short. It must be at least 32 characters long.");
                    throw new InvalidOperationException("JWT Key is too short.");
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var expiry = DateTime.UtcNow.AddHours(3);

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: expiry,
                    signingCredentials: creds
                );

                _logger.LogInformation("JWT token created for user {Email}, expires at {Expiry}.", user.Email, expiry);

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating JWT token.");
                throw;
            }
        }

        public RefreshToken CreateRefreshToken(Guid userId, string? ip, string? userAgent)
        {
            try
            {
                var tokenString = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                var expiry = DateTime.UtcNow.AddDays(7);

                var refreshToken = new RefreshToken
                {
                    UserId = userId,
                    Token = tokenString,
                    ExpiryDate = expiry,
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = ip,
                    UserAgent = userAgent,
                    IsRevoked = false
                };

                _logger.LogInformation("Generated refresh token for user {UserId}. Expires at {Expiry}.", userId, expiry);
                _logger.LogDebug("Refresh token metadata: IP={IP}, UserAgent={UA}", ip, userAgent);

                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating refresh token for user {UserId}.", userId);
                throw;
            }
        }
    }
}
