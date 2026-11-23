namespace Fundoo.User.Domain.Entities;

public class ExternalLogin
{
    //external login id
    public Guid Id { get; set; }

    //users table id 
    public Guid UserId { get; set; }

    // For example, "Google".
    public string Provider { get; set; } = "";

    // Google’s stable "sub" claim.
    public string ProviderSubject { get; set; } = "";
}