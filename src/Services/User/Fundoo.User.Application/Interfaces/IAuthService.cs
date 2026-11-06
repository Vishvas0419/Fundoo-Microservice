
namespace Fundoo.User.Application.Interfaces;

public interface IAuthService
{
    Task<string> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<string?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<string?> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}
