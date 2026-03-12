using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Industriall.MaintOps.Api.Infrastructure.Security;

/// <summary>
/// Generates signed JWT bearer tokens.
/// </summary>
public sealed class JwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
        => _configuration = configuration;

    public string GenerateToken(Guid userId, string email, IEnumerable<string>? roles = null)
    {
        var secretKey    = _configuration["Jwt:SecretKey"]
                           ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
        var issuer       = _configuration["Jwt:Issuer"]!;
        var audience     = _configuration["Jwt:Audience"]!;
        var expiresInHrs = int.Parse(_configuration["Jwt:ExpirationInHours"] ?? "1");

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (roles is not null)
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(expiresInHrs),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
