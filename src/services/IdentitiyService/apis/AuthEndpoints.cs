using System.Security.Claims;
using IdentitiyService.Interfaces;
using IdentitiyService.Models.Dtos;
using IdentitiyService.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace IdentitiyService.Apis;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/").WithOpenApi();

        group.MapPost("/register", async (RegisterReq request, IAuthenticationService authService) =>
        {
            var result = await authService.RegisterAsync(request);
            return result != null ? Results.Ok(result) : Results.BadRequest("User already exists or invalid data.");
        });

        group.MapPost("/login", async (LoginReq request, IAuthenticationService authService) =>
        {
            var result = await authService.LoginAsync(request);
            return result != null ? Results.Ok(result) : Results.Unauthorized();
        });

        group.MapPost("/validate", (ValidateReq req, HttpContext context) =>
        {
            var userIdClaim = context.User.FindFirst("userId")?.Value;
            var roleClaim = context.User.FindFirst("role")?.Value;

            if (userIdClaim == null || roleClaim == null) return Results.Unauthorized();
            
            if (userIdClaim != req.UserId.ToString() || roleClaim != ((int)req.Role).ToString())
                return Results.Unauthorized();

            return Results.Ok(new { isValid = true });
        }).RequireAuthorization();
    }
}