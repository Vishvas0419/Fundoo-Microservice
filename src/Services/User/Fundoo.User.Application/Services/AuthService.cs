using Fundoo.User.Application.Interfaces;
using Fundoo.User.Domain.Entities;

namespace Fundoo.User.Application.Services;

//simply asks the required interfaces to perform those operations.
//contains Registration and login business logic
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IPasswordResetTokenService _resetTokenService;
    private readonly INotificationClient _notificationClient;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IPasswordResetTokenService resetTokenService,
        INotificationClient notificationClient  )
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _resetTokenService = resetTokenService;
        _notificationClient = notificationClient;
    }

    public async Task<string> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        //check if user already exists
        var existingUser =
            await _userRepository.GetByNormalizedEmailAsync(
                normalizedEmail, cancellationToken);

        if (existingUser != null) 
        {
            throw new InvalidOperationException(
                "An account with this email already exists.");
        }

        var now = DateTime.UtcNow;
        //if user doesnt already exists, put the user info inside a AppUser 
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(password), //storing password in hashed form
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        //adding the registered AppUser to DB
        await _userRepository.AddAsync(user,cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return "User registered successfully.";
    }

    public async Task<string?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        //check if the entered normalizedEmail is present in the db or not
        var user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail,cancellationToken);

        if (user == null || !user.IsActive)
        {
            return null;
        }

        //compares password and verfies it with the given stored hash
        var isPasswordValid = _passwordHasher.Verify(password,user.PasswordHash); 

        if (!isPasswordValid)
        {
            return null;
        }
        
        await _notificationClient.SendEmailAsync(user.Email, $"Welcome {user.FirstName}",$"Hello {user.FirstName} welcome to fundoo app",cancellationToken);

        //if the email and  pasword is authenticated then return the access token as a response
        return _tokenService.CreateAccessToken(user);
    }

    public async Task<string?> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        var user =
            await _userRepository.GetByNormalizedEmailAsync(
                normalizedEmail, cancellationToken);

        // Do not reveal whether this email belongs to an account.
        if (user == null || !user.IsActive)
        {
            return null;
        }

        await _userRepository.InvalidateUnusedPasswordResetTokensAsync(user.Id,DateTime.UtcNow,cancellationToken);

        await _userRepository.SaveChangesAsync(cancellationToken);

        var rawToken = _resetTokenService.GenerateToken();//This is the token sent to the user by email.
        var tokenHash = _resetTokenService.HashToken(rawToken);//creates a one-way hash of that token. The hash is stored in the database:

        //password reset token entity for storing in DB
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
            UsedAtUtc = null
        };

        //adding password reset token entity to DB
        await _userRepository.AddPasswordResetTokenAsync(resetToken, cancellationToken);

        await _userRepository.SaveChangesAsync(cancellationToken);

        // Email integration will be added later.
        // For now, the raw token is not returned or stored.
        //var resetLink = $"https://localhost:5134/reset-password?token={Uri.EscapeDataString(rawToken)}";

        //This URL-encodes the raw reset token so it can be safely placed inside a URL, query string, or email content.
        var resetPasswordToken = Uri.EscapeDataString(rawToken); //Uri.EscapeDataString converts unsafe characters into percent-encoded form:

        //sending resetPasswordToken to gmail
        await _notificationClient.SendEmailAsync(
            user.Email,
            "Reset your Fundoo password",
            $"Hello {user.FirstName},\n\n" +
            $"Use the following token to reset your password:\n{resetPasswordToken}\n\n" +
            "This token will get expire in 15 minutes. If you did not request a password reset, you can ignore this email.",
            cancellationToken);

        return null;

        //return rawToken;
    }

    public async Task<bool> ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            string.IsNullOrWhiteSpace(newPassword))
        {
            return false;
        }

        var tokenHash = _resetTokenService.HashToken(token);

        var resetToken =
            await _userRepository.GetPasswordResetTokenByHashAsync(
                tokenHash, cancellationToken);

        if (resetToken == null ||
            resetToken.UsedAtUtc != null ||
            resetToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return false;
        }

        var user = await _userRepository.GetByIdAsync(
            resetToken.UserId, cancellationToken);

        if (user == null || !user.IsActive)
        {
            return false;
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;

        resetToken.UsedAtUtc = DateTime.UtcNow;

        await _userRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
