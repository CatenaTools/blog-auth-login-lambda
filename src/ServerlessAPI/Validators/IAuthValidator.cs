using Models;

namespace ServerlessAPI.Validators;

public interface IAuthValidator
{
    public string GenerateAuthUrl(string publicBase);
    public GenericProviderUserData HandleOAuthCallback(string code, string redirectUri);
}