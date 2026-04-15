using System.Security.Claims;
using IdentitiyService.Interfaces;
using IdentitiyService.Models.Dtos;

namespace IdentitiyService.Apis;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/").RequireAuthorization().WithOpenApi();

        group.MapGet("/profile", async (HttpContext context, IProfileService profileService) =>
        {
            var userIdStr = context.User.FindFirst("userId")?.Value;
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            // Allow requesting another user's profile via query param
            if (context.Request.Query.TryGetValue("targetUserId", out var targetUserIdStr) && Guid.TryParse(targetUserIdStr, out var targetUserId))
            {
                userId = targetUserId;
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