using System.Security.Claims;
using IdentitiyService.Interfaces;
using IdentitiyService.Models.Dtos;

namespace IdentitiyService.Apis;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/").RequireAuthorization().WithOpenApi();

        // Allow anonymous requests for specific public profile data
        routes.MapGet("/public-profile", async (HttpContext context, IProfileService profileService) =>
        {
            if (context.Request.Query.TryGetValue("targetId", out var targetIdStr))
            {
                if (Guid.TryParse(targetIdStr, out var targetUserId))
                {
                    var profileById = await profileService.GetProfileAsync(targetUserId);
                    return profileById != null ? Results.Ok(profileById) : Results.NotFound();
                }
            }
            return Results.BadRequest();
        }).WithOpenApi();

        group.MapGet("/profile", async (HttpContext context, IProfileService profileService) =>
        {
            var userIdStr = context.User.FindFirst("userId")?.Value;
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            // Allow requesting another user's profile via query param (by ID or Email)
            if (context.Request.Query.TryGetValue("targetId", out var targetIdStr))
            {
                if (Guid.TryParse(targetIdStr, out var targetUserId))
                {
                    var profileById = await profileService.GetProfileAsync(targetUserId);
                    return profileById != null ? Results.Ok(profileById) : Results.NotFound();
                }
                else
                {
                    var profileByEmail = await profileService.GetProfileByEmailAsync(targetIdStr.ToString());
                    return profileByEmail != null ? Results.Ok(profileByEmail) : Results.NotFound();
                }
            }

            var profile = await profileService.GetProfileAsync(userId);
            return profile != null ? Results.Ok(profile) : Results.NotFound();
        });

        group.MapPost("/update-profile", async (UpdateProfileReq request, HttpContext context, IProfileService profileService) =>
        {
            var userIdStr = context.User.FindFirst("userId")?.Value;
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var updated = await profileService.UpdateProfileAsync(userId, request);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        });
    }
}