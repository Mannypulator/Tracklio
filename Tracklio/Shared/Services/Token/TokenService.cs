using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tracklio.Shared.Configurations;

namespace Tracklio.Shared.Services.Token;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateAccessToken(Guid userId, string email, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public Task<string> GenerateEmailConfirmationTokenAsync(Guid userId)
    {
        var token = GenerateSecureToken(userId, "email_confirmation");
        return Task.FromResult(token);
    }

    public Task<string> GeneratePasswordResetTokenAsync(Guid userId)
    {
        var token = GenerateSecureToken(userId, "password_reset");
        return Task.FromResult(token);
    }

    public Task<bool> ValidateEmailConfirmationTokenAsync(Guid userId, string token)
    {
        return Task.FromResult(ValidateSecureToken(userId, token, "email_confirmation"));
    }

    public Task<bool> ValidatePasswordResetTokenAsync(Guid userId, string token)
    {
        return Task.FromResult(ValidateSecureToken(userId, token, "password_reset"));
    }

    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }

    public bool IsTokenExpired(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    private string GenerateSecureToken(Guid userId, string purpose)
    {
        var data = $"{userId}:{purpose}:{DateTime.UtcNow.AddHours(24):O}";
        var bytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var hash = hmac.ComputeHash(bytes);
        return Convert.ToBase64String(bytes.Concat(hash).ToArray());
    }

    private bool ValidateSecureToken(Guid userId, string token, string purpose)
    {
        try
        {
            var tokenBytes = Convert.FromBase64String(token);
            var dataBytes = tokenBytes.Take(tokenBytes.Length - 32).ToArray();
            var hashBytes = tokenBytes.Skip(tokenBytes.Length - 32).ToArray();

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var computedHash = hmac.ComputeHash(dataBytes);

            if (!computedHash.SequenceEqual(hashBytes))
                return false;

            var data = Encoding.UTF8.GetString(dataBytes);
            var parts = data.Split(':');

            if (parts.Length != 3)
                return false;

            if (!Guid.TryParse(parts[0], out var tokenUserId) || tokenUserId != userId)
                return false;

            if (parts[1] != purpose)
                return false;

            if (!DateTime.TryParse(parts[2], out var expiry) || expiry < DateTime.UtcNow)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }
}