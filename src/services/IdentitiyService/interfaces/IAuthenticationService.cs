using IdentitiyService.Models.Dtos;

namespace IdentitiyService.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResponse?> RegisterAsync(RegisterReq request);
    Task<AuthResponse?> LoginAsync(LoginReq request);
}