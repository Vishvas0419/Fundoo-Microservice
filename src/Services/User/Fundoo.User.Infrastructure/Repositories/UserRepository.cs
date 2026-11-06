
using Fundoo.User.Application.Interfaces;
using Fundoo.User.Domain.Entities;
using Fundoo.User.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

//implement the user repo interfaces methods
//and manage Database operations

namespace Fundoo.User.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserDbContext _context;

    public UserRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);
    }

    public async Task<AppUser?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        AppUser user,
        CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPasswordResetTokenAsync(
        PasswordResetToken resetToken,
        CancellationToken cancellationToken = default)
    {
        await _context.PasswordResetTokens.AddAsync(
            resetToken,
            cancellationToken);
    }

    public async Task<PasswordResetToken?> GetPasswordResetTokenByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);
    }


    public async Task InvalidateUnusedPasswordResetTokensAsync(
        Guid userId,
        DateTime usedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var unusedTokens = await _context.PasswordResetTokens
            .Where(token =>
                token.UserId == userId &&
                token.UsedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in unusedTokens)
        {
            token.UsedAtUtc = usedAtUtc;
        }
    }

}
