using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DoughDoodle.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DoughDoodle.Services.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Key { get; set; } = "development-only-placeholder-not-a-secret";
    public string Issuer { get; set; } = "DoughDoodle";
    public string Audience { get; set; } = "DoughDoodleApi";
    public int ExpiryMinutes { get; set; } = 60;
}

public interface IJwtTokenService
{
    string CreateToken(ApplicationUser user);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opts;

    public JwtTokenService(IOptions<JwtOptions> opts) => _opts = opts.Value;

    public string CreateToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.UserName ?? ""),
            new("IsAdmin", user.IsAdmin ? "true" : "false")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opts.ExpiryMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
