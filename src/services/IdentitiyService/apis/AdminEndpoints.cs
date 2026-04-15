using IdentitiyService.Data;
using IdentitiyService.Models;
using IdentitiyService.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using IdentitiyService.Interfaces;

namespace IdentitiyService.Apis;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/admin").RequireAuthorization();

        // Middleware to enforce Admin role for all routes in this group
        group.AddEndpointFilter(async (context, next) =>
        {
            var roleClaim = context.HttpContext.User.FindFirst("role")?.Value 
                            ?? context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;

            if (roleClaim != ((int)UserRole.Admin).ToString())
            {
                return Results.Forbid();
            }
            return await next(context);
        });

        // 1. GET /admin/users - Get all users
        group.MapGet("/users", async (IdentityDbContext db) =>
        {
            var users = await db.Users
                .Include(u => u.Profile)
                .Select(u => new
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = (int)u.Role,
                    FirstName = u.Profile != null ? u.Profile.FirstName : "",
                    LastName = u.Profile != null ? u.Profile.LastName : "",
                    Description = u.Profile != null ? u.Profile.Description : ""
                })
                .ToListAsync();

            return Results.Ok(users);
        });

        // 2. POST /admin/user - Create a user
        group.MapPost("/user", async ([FromBody] AdminCreateUserReq req, IAuthenticationService authService, IdentityDbContext db) =>
        {
            // Re-use register logic for simplicity, then override role/profile
            var registerReq = new Models.Dtos.RegisterReq(
                req.Email,
                req.Password ?? "Default123!",
                req.FirstName ?? "",
                req.LastName ?? ""
            );

            var registerResult = await authService.RegisterAsync(registerReq);
            if (registerResult == null) return Results.BadRequest("User already exists or creation failed.");

            // Find created user and update their role/description
            var user = await db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Email == req.Email);
            if (user != null)
            {
                user.Role = (UserRole)req.Role;
                if (user.Profile != null)
                {
                    user.Profile.Description = req.Description ?? "";
                }
                await db.SaveChangesAsync();
                
                return Results.Ok(new
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = (int)user.Role,
                    FirstName = user.Profile?.FirstName,
                    LastName = user.Profile?.LastName,
                    Description = user.Profile?.Description
                });
            }

            return Results.BadRequest("Failed to create user.");
        });

        // 3. PUT /admin/user/{id} - Update a user
        group.MapPut("/user/{id}", async (Guid id, [FromBody] AdminUpdateUserReq req, IdentityDbContext db, IAuthenticationService authService) =>
        {
            var user = await db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return Results.NotFound("User not found.");

            user.Email = req.Email ?? user.Email;
            if (req.Role.HasValue) user.Role = (UserRole)req.Role.Value;
            
            if (user.Profile != null)
            {
                user.Profile.FirstName = req.FirstName ?? user.Profile.FirstName;
                user.Profile.LastName = req.LastName ?? user.Profile.LastName;
                user.Profile.Description = req.Description ?? user.Profile.Description;
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(req.Password))
            {
                var pwdAuth = await db.PasswordAuths.FirstOrDefaultAsync(pa => pa.UserId == user.Id);
                if (pwdAuth != null)
                {
                    pwdAuth.PasswordHash = authService.CreatePasswordHash(req.Password);
                }
                else
                {
                    db.PasswordAuths.Add(new PasswordAuth
                    {
                        UserId = user.Id,
                        PasswordHash = authService.CreatePasswordHash(req.Password)
                    });
                }
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                Id = user.Id,
                Email = user.Email,
                Role = (int)user.Role,
                FirstName = user.Profile?.FirstName,
                LastName = user.Profile?.LastName,
                Description = user.Profile?.Description
            });
        });

        // 4. DELETE /admin/user/{id} - Delete a user
        group.MapDelete("/user/{id}", async (Guid id, IdentityDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user == null) return Results.NotFound("User not found.");
            
            // EF Core will cascade delete Profile and PasswordAuth due to FK
            db.Users.Remove(user);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "User deleted successfully" });
        });
    }
}

public class AdminCreateUserReq
{
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public int Role { get; set; } = 0;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Description { get; set; }
}

public class AdminUpdateUserReq
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public int? Role { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Description { get; set; }
}