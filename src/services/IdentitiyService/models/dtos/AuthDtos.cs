using IdentitiyService.Models.Enums;

namespace IdentitiyService.Models.Dtos;

public record RegisterReq(string Email, string Password, string FirstName, string LastName);

public record LoginReq(string Email, string Password);

public record GoogleAuthReq(string IdToken);

public record UpdateProfileReq(string FirstName, string LastName, string Description, string ProfileUrl);

public record AuthResponse(string Token, string Email, Guid UserId, UserRole Role, string ProfileUrl);

public record ProfileResponse(Guid UserId, string FirstName, string LastName, string Description, string ProfileUrl);

public record ValidateReq(Guid UserId, UserRole Role);
