
namespace Fundoo.User.Api.Contracts;

public class AuthResponse
{
    public string Message { get; set; } = "";

    public string? AccessToken { get; set; }

    public string? ResetToken { get; set; }
}
