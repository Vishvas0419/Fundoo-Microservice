
namespace Fundoo.User.Api.Contracts;


//When the client sends a login request, the JSON body might look like this: 
public class LoginRequest
{
    public string Email { get; set; } = "";

    public string Password { get; set; } = "";
}


//ASP.NET Core binds this JSON to LoginRequest.
//The controller will pass the email and password to IAuthService.LoginAsync(). The service finds the user, verifies the password hash using BCrypt, and returns a JWT if authentication succeeds.