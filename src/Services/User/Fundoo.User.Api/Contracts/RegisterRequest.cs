
namespace Fundoo.User.Api.Contracts;

//When a client sends a registration request to POST /api/auth/register, the request body will contain these four fields.
public class RegisterRequest
{
    public string FirstName { get; set; } = "";

    public string LastName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Password { get; set; } = "";
}
