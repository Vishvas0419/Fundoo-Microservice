
using Fundoo.User.Domain.Entities;

namespace Fundoo.User.Application.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByNormalizedEmailAsync(string normalizedEmail,CancellationToken cancellationToken = default);
    Task<AppUser?> GetByIdAsync(Guid id,CancellationToken cancellationToken = default);
    Task AddAsync(AppUser user,CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Password reset token operations
    Task AddPasswordResetTokenAsync(PasswordResetToken resetToken,CancellationToken cancellationToken = default);
    Task<PasswordResetToken?> GetPasswordResetTokenByHashAsync(string tokenHash,CancellationToken cancellationToken = default);
    Task InvalidateUnusedPasswordResetTokensAsync(Guid userId,DateTime usedAtUtc,CancellationToken cancellationToken = default);


    Task<ExternalLogin?> GetExternalLoginAsync(
    string provider,
    string providerSubject,
    CancellationToken cancellationToken = default);

    Task AddExternalLoginAsync(
        ExternalLogin externalLogin,
        CancellationToken cancellationToken = default);
}
