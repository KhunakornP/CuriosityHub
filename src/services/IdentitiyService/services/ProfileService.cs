using System.Security.Claims;
using IdentitiyService.Data;
using IdentitiyService.Interfaces;
using IdentitiyService.Models;
using IdentitiyService.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentitiyService.Services;

public class ProfileService : IProfileService
{
    private readonly IdentityDbContext _context;

    public ProfileService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<ProfileResponse?> GetProfileAsync(Guid userId)
    {
        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
        return profile != null ? new ProfileResponse(profile.UserId, profile.FirstName, profile.LastName, profile.Description, profile.ProfileUrl) : null;
    }

    public async Task<ProfileResponse?> GetProfileByEmailAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return null;
        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        return profile != null ? new ProfileResponse(profile.UserId, profile.FirstName, profile.LastName, profile.Description, profile.ProfileUrl) : null;
    }

    public async Task<ProfileResponse?> UpdateProfileAsync(Guid userId, UpdateProfileReq request)
    {
        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return null;

        profile.FirstName = request.FirstName;
        profile.LastName = request.LastName;
        profile.Description = request.Description;
        profile.ProfileUrl = request.ProfileUrl;

        await _context.SaveChangesAsync();
        return new ProfileResponse(profile.UserId, profile.FirstName, profile.LastName, profile.Description, profile.ProfileUrl);
    }
}