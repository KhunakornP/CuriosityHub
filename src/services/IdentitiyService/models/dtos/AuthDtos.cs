using IdentitiyService.Models.Enums;

namespace IdentitiyService.Models.Dtos;

public record RegisterReq(string Email, string Password, string FirstName, string LastName);

public record LoginReq(string Email, string Password);

public record UpdateProfileReq(string FirstName, string LastName, string Description);

public record AuthResponse(string Token, string Email, Guid UserId, UserRole Role);

public record ProfileResponse(Guid UserId, string FirstName, string LastName, string Description);

public record ValidateReq(Guid UserId, UserRole Role);
