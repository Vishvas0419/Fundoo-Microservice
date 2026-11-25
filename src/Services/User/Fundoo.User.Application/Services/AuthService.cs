using Fundoo.User.Application.Interfaces;
using Fundoo.User.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Reflection.Emit;
using System.Security.Cryptography;
using Fundoo.Contracts.Events;

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
    private readonly ILogger<AuthService> _logger;
    private readonly IEventPublisher _eventPublisher;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IPasswordResetTokenService resetTokenService,
        INotificationClient notificationClient,
        IEventPublisher eventPublisher,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _resetTokenService = resetTokenService;
        _notificationClient = notificationClient;
        _eventPublisher = eventPublisher;
        _logger = logger;
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
            _logger.LogWarning("Registration rejected because the account already exists");
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

        //create a registration event 
        var userRegisteredEvent = new UserRegisteredEvent
        {
            EventId = Guid.NewGuid(),
            EventType = "UserRegistered",
            EventVersion = 1,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        //publish the event to kafka
        await _eventPublisher.PublishAsync(
            "user-registered",
            userRegisteredEvent,
            cancellationToken);

        _logger.LogInformation("User account {UserId} registered successfully", user.Id);

        //await _notificationClient.SendEmailAsync(user.Email, $"Welcome {user.FirstName}", $"Hello {user.FirstName} welcome to fundoo app", cancellationToken);

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
            _logger.LogWarning("Login rejected for an unknown or inactive account");
            return null;
        }

        //compares password and verfies it with the given stored hash
        var isPasswordValid = _passwordHasher.Verify(password,user.PasswordHash); 

        if (!isPasswordValid)
        {
            _logger.LogWarning("Login rejected because the supplied credentials were invalid");
            return null;
        }
        
        //await _notificationClient.SendEmailAsync(user.Email, $"Welcome {user.FirstName}",$"Hello {user.FirstName} welcome to fundoo app",cancellationToken);

        //if the email and  pasword is authenticated then return the access token as a response
        var accessToken = _tokenService.CreateAccessToken(user);
        _logger.LogInformation("User {UserId} authenticated successfully", user.Id);
        return accessToken;
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

        _logger.LogInformation("Password reset instructions sent for user {UserId}", user.Id);

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

        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);

        return true;
    }

    //google Auth 
    //The method returns the Fundoo access token after resolving the local account.
    //This reuses the existing Fundoo JWT generator.The random password is never returned or stored in plaintext; it only keeps the current user schema compatible with Google-only accounts.

    public async Task<string> SignInWithExternalProviderAsync(
    string provider, //identifies the provider, for example "Google"
    string providerSubject, //the sub claim.
    string email, //email from the provider
    string? displayName,
    CancellationToken cancellationToken = default)
    {
        //Checks that the three required identity values are not null, empty, or whitespace.
        if (string.IsNullOrWhiteSpace(provider) ||
            string.IsNullOrWhiteSpace(providerSubject) ||
            string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "External provider, subject, and email are required.");
        }

        provider = provider.Trim();
        providerSubject = providerSubject.Trim();
        email = email.Trim();

        //Searches the ExternalLogins table for this provider and provider subject.
        var externalLogin = await _userRepository.GetExternalLoginAsync(
            provider,
            providerSubject,
            cancellationToken);

        //Checks whether the lookup found a matching external-login row.
        if (externalLogin is not null)
        {//Reads the UserId from the externalLogins table and gets the assosiated Fundoo user from the Users table.
            var linkedUser = await _userRepository.GetByIdAsync(
                externalLogin.UserId,
                cancellationToken);

            if (linkedUser is null || !linkedUser.IsActive)
            {
                throw new InvalidOperationException(
                    "The linked Fundoo account is unavailable.");
            }
            //Creates and returns a Fundoo access token for the already-linked user means the user which is already present in the ExternalLogins table.
            return _tokenService.CreateAccessToken(linkedUser);
        }//Ends the “Google identity already linked” branch.


        //If there was no external-login row entry in ExternalLogins table, the execution continues to the email lookup.
        var normalizedEmail = email.ToUpperInvariant();

        //Searches the Users table for an account whose normalized email matches the provider’s email.
        var existingUser = await _userRepository.GetByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        //if the provider email is present in the users table, add that user as an external login entity it to ExternalLogins table with the specified ID,
        if (existingUser is not null)
        {
            if (!existingUser.IsActive)
            {
                throw new InvalidOperationException(
                    "The Fundoo account is unavailable.");
            }
            //Important!
            //Add an ExternalLogin with UserId = existingUser.Id, plus the provider name and Google subject. That associates this Google identity with the existing Fundoo user.
            await _userRepository.AddExternalLoginAsync(
                new ExternalLogin
                {
                    Id = Guid.NewGuid(),
                    UserId = existingUser.Id,
                    Provider = provider, //"Google"
                    ProviderSubject = providerSubject //Google’s stable "sub" claim.
                },
                cancellationToken);

            //save the external login to ExternalLogins table
            await _userRepository.SaveChangesAsync(cancellationToken);

            //return the access token linked with the current user
            return _tokenService.CreateAccessToken(existingUser);
        }

        var nameParts = (displayName ?? "").Trim()
            .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        var firstName = nameParts.Length > 0 ? nameParts[0] : email;
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        // Generate an unknown random password so this Google-only account
        // satisfies the existing non-null PasswordHash schema.
        var randomPassword = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(48));

        var now = DateTime.UtcNow;

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(randomPassword),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _userRepository.AddAsync(user, cancellationToken);

        await _userRepository.AddExternalLoginAsync(
            new ExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = provider,
                ProviderSubject = providerSubject
            },
            cancellationToken);

        // EF Core SaveChanges persists both rows together.
        await _userRepository.SaveChangesAsync(cancellationToken);

        return _tokenService.CreateAccessToken(user);
    }
}
