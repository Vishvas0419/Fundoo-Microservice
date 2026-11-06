
using System.Security.Claims;
using System.Text;
using Fundoo.User.Application.Interfaces;
using Fundoo.User.Domain.Entities;
using Microsoft.Extensions.Configuration;
//using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;


namespace Fundoo.User.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateAccessToken(AppUser user)
    {
        var signingKey = _configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException(
                "JWT signing key is not configured.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is not configured.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is not configured.");

        var expiryMinutes = int.TryParse(
            _configuration["Jwt:ExpiryMinutes"],
            out var minutes)
                ? minutes
                : 30;

        // 1. Create the security key
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingKey));

        // 2. Create signing credentials
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // 3. Create claims
        var claims = new[]
        {
        new Claim(
            JwtRegisteredClaimNames.Sub,
            user.Id.ToString()),

        new Claim(
            JwtRegisteredClaimNames.Email,
            user.Email)
    };

        // 4. Create JWT token
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        // 5. Convert JWT object into a string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }



    public string CreatePasswordResetToken(AppUser user)
    {
        var signingKey = _configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException(
                "JWT signing key is not configured.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is not configured.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is not configured.");

        // 1. Create security key
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingKey));

        // 2. Create signing credentials
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // 3. Create claims
        var claims = new[]
        {
        new Claim(
            JwtRegisteredClaimNames.Sub,
            user.Id.ToString()),

        new Claim(
            JwtRegisteredClaimNames.Email,
            user.Email),

        new Claim(
            "token_type",
            "password_reset")
    };

        // 4. Create JWT token
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        // 5. Convert JWT object into string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    //This uses the same signing key, issuer and audience as your existing access token, but adds a token_type claim and uses a 15-minute expiry. The reset endpoint must explicitly validate this token type and must not accept an ordinary access token as a password-reset token.


}
