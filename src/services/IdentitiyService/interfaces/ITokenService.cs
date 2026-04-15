using IdentitiyService.Models;

namespace IdentitiyService.Interfaces;

public interface ITokenService
{
    string GenerateJwtToken(User user);
}