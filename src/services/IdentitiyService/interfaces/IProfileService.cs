using IdentitiyService.Models.Dtos;

namespace IdentitiyService.Interfaces;

public interface IProfileService
{
    Task<ProfileResponse?> GetProfileAsync(Guid userId);
    Task<ProfileResponse?> UpdateProfileAsync(Guid userId, UpdateProfileReq request);
}