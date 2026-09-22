using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Features.Auth;

public class AuthService(
    AppDbContext db,
    TokenService tokenService,
    IPasswordHasher<User> passwordHasher,
    CurrentUser currentUser)
{
    private const string InvalidCredentials = "Invalid email or password.";
    private const string SessionExpired = "Your session has expired. Please sign in again.";

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new ConflictException("An account with this email already exists.");

        var user = new User { Email = email, FullName = request.FullName.Trim() };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);

        try
        {
            return await StartSessionAsync(user);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Two registrations with the same email raced past the check above.
            throw new ConflictException("An account with this email already exists.");
        }
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email)
            ?? throw new UnauthorizedException(InvalidCredentials);

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedException(InvalidCredentials);

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        return await StartSessionAsync(user);
    }

    // Refresh tokens are single use: every refresh revokes the old token and issues a new one.
    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var hash = TokenService.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash);

        if (stored == null || !stored.IsActive)
            throw new UnauthorizedException(SessionExpired);

        stored.RevokedAt = DateTime.UtcNow;
        return await StartSessionAsync(stored.User);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = TokenService.HashRefreshToken(refreshToken);

        await db.RefreshTokens
            .Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));
    }

    public async Task<UserDto> GetCurrentUserAsync()
    {
        return await db.Users
            .Where(u => u.Id == currentUser.Id)
            .Select(u => new UserDto(u.Id, u.Email, u.FullName))
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("User not found.");
    }

    private async Task<AuthResult> StartSessionAsync(User user)
    {
        var refreshToken = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            User = user,
            TokenHash = TokenService.HashRefreshToken(refreshToken.Value),
            ExpiresAt = refreshToken.ExpiresAt
        });

        await db.SaveChangesAsync();

        // Created after saving so a newly registered user already has an Id.
        var accessToken = tokenService.CreateAccessToken(user);
        var response = new AuthResponse(accessToken.Value, accessToken.ExpiresAt, new UserDto(user.Id, user.Email, user.FullName));

        return new AuthResult(response, refreshToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
