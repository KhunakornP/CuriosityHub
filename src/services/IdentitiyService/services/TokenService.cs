using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentitiyService.Interfaces;
using IdentitiyService.Models;
using Microsoft.IdentityModel.Tokens;

namespace IdentitiyService.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateJwtToken(User user)
    {
        var secret = _configuration["Jwt:Secret"] ?? throw new ArgumentNullException("Jwt:Secret");
        var issuer = _configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("userId", user.Id.ToString()),
            new Claim("role", ((int)user.Role).ToString()),
            new Claim("email", user.Email),
            new Claim(JwtRegisteredClaimNames.Iss, issuer)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(3),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}