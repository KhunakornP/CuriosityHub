using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;
using IdentitiyService.Data;
using IdentitiyService.Interfaces;
using IdentitiyService.Models;
using IdentitiyService.Models.Dtos;
using IdentitiyService.Models.Enums;
using Konscious.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace IdentitiyService.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IdentityDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AuthenticationService(IdentityDbContext context, ITokenService tokenService, IConfiguration configuration)
    {
        _context = context;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterReq request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return null; // User exists

        var user = new User { Email = request.Email, Role = UserRole.Viewer };
        var salt = CreateSalt();
        var hashedPwd = HashPassword(request.Password, salt);
        var storedHash = Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hashedPwd);

        var passwordAuth = new PasswordAuth
        {
            UserId = user.Id,
            PasswordHash = storedHash,
            User = user
        };

        var profile = new Profile
        {
            UserId = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            User = user
        };

        _context.Users.Add(user);
        _context.PasswordAuths.Add(passwordAuth);
        _context.Profiles.Add(profile);

        await _context.SaveChangesAsync();

        var token = _tokenService.GenerateJwtToken(user);
        return new AuthResponse(token, user.Email, user.Id, user.Role, profile.ProfileUrl);
    }

    public async Task<AuthResponse?> LoginAsync(LoginReq request)
    {
        var user = await _context.Users
            .Include(u => u.PasswordAuth)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || user.PasswordAuth == null)
            return null;

        var parts = user.PasswordAuth.PasswordHash.Split(':');
        if (parts.Length != 2) return null;

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = HashPassword(request.Password, salt);

        if (!actualHash.SequenceEqual(expectedHash))
            return null;

        var token = _tokenService.GenerateJwtToken(user);
        return new AuthResponse(token, user.Email, user.Id, user.Role, user.Profile?.ProfileUrl ?? "");
    }

    public async Task<AuthResponse?> GoogleLoginAsync(GoogleAuthReq request)
    {
        var clientId = _configuration["Google:ClientId"];
        GoogleJsonWebSignature.Payload payload;

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch
        {
            return null; // Invalid token
        }

        var googleAuth = await _context.GoogleAuths
            .Include(ga => ga.User)
            .ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(ga => ga.GoogleId == payload.Subject);

        if (googleAuth != null && googleAuth.User != null)
        {
            var token = _tokenService.GenerateJwtToken(googleAuth.User);
            return new AuthResponse(token, googleAuth.User.Email, googleAuth.User.Id, googleAuth.User.Role, googleAuth.User.Profile?.ProfileUrl ?? "");
        }

        // Auto-register if not found
        var user = await _context.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Email == payload.Email);
        if (user == null)
        {
            user = new User { Email = payload.Email, Role = UserRole.Viewer };
            var profile = new Profile
            {
                UserId = user.Id,
                FirstName = payload.GivenName ?? "",
                LastName = payload.FamilyName ?? "",
                ProfileUrl = payload.Picture ?? "",
                User = user
            };
            _context.Users.Add(user);
            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        var newGoogleAuth = new GoogleAuth
        {
            GoogleId = payload.Subject,
            UserId = user.Id,
            User = user
        };
        _context.GoogleAuths.Add(newGoogleAuth);
        await _context.SaveChangesAsync();

        var newToken = _tokenService.GenerateJwtToken(user);
        return new AuthResponse(newToken, user.Email, user.Id, user.Role, user.Profile?.ProfileUrl ?? "");
    }

    private byte[] CreateSalt()
    {
        var buffer = new byte[16];
        RandomNumberGenerator.Fill(buffer);
        return buffer;
    }

    private byte[] HashPassword(string password, byte[] salt)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 8,
            Iterations = 4,
            MemorySize = 1024 * 128 // 128 MB
        };
        return argon2.GetBytes(16);
    }
}