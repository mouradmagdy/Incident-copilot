using System.Net.Mail;
using IncidentCopilot.Api.Contracts.Auth;
using IncidentCopilot.Api.Security;
using IncidentCopilot.Core.Entities;
using IncidentCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IncidentCopilot.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    public const string AccessTokenCookieName =
        "incident_copilot_access_token";

    private readonly IncidentCopilotDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtTokenService _tokens;

    public AuthController(
        IncidentCopilotDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        JwtTokenService tokens)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthUserResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateRegistration(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(
                validationErrors));
        }

        var email = request.Email.Trim();
        var normalizedEmail = NormalizeEmail(email);
        var exists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (exists)
        {
            return Conflict(new ProblemDetails
            {
                Title = "An account with this email already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            return Conflict(new ProblemDetails
            {
                Title = "An account with this email already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        IssueCookie(user);
        return Ok(Map(user));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthUserResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrEmpty(request.Password))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "The email or password is incorrect.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _dbContext.Users.SingleOrDefaultAsync(
            item => item.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (user is null ||
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "The email or password is incorrect.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        IssueCookie(user);
        return Ok(Map(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserResponse>> Me(
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new AuthUserResponse(
                item.Id,
                item.DisplayName,
                item.Email))
            .SingleOrDefaultAsync(cancellationToken);

        return user is null ? Unauthorized() : Ok(user);
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(
            AccessTokenCookieName,
            BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
        return NoContent();
    }

    private void IssueCookie(User user)
    {
        var token = _tokens.Create(user);
        Response.Cookies.Append(
            AccessTokenCookieName,
            token.Value,
            BuildCookieOptions(token.ExpiresAtUtc));
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset expires)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            Path = "/",
            Expires = expires
        };
    }

    private static AuthUserResponse Map(User user)
    {
        return new AuthUserResponse(
            user.Id,
            user.DisplayName,
            user.Email);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static Dictionary<string, string[]> ValidateRegistration(
        RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var displayName = request.DisplayName?.Trim() ?? string.Empty;

        if (displayName.Length is < 2 or > 80)
        {
            errors[nameof(request.DisplayName)] =
                ["Display name must be between 2 and 80 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.Email) ||
            request.Email.Length > 254 ||
            !MailAddress.TryCreate(request.Email.Trim(), out _))
        {
            errors[nameof(request.Email)] =
                ["Enter a valid email address."];
        }

        if (string.IsNullOrEmpty(request.Password) ||
            request.Password.Length < 10 ||
            !request.Password.Any(char.IsUpper) ||
            !request.Password.Any(char.IsLower) ||
            !request.Password.Any(char.IsDigit))
        {
            errors[nameof(request.Password)] =
            [
                "Password must be at least 10 characters and include uppercase, lowercase, and a number."
            ];
        }

        return errors;
    }
}
