using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Models;

namespace ServerlessAPI.Validators;

public class DiscordAuthValidator(string client_id, string client_secret) : IAuthValidator
{
    private const string DiscordAuthorizeUrl = "https://discord.com/api/oauth2/authorize";
    private const string DiscordTokenUrl = "https://discord.com/api/oauth2/token";
    private const string DiscordUserUrl = "https://discord.com/api/users/@me";

    private string _clientID = client_id;
    private string _clientSecret = client_secret;

    public string GenerateAuthUrl(string publicBase)
    {
        var par = new Dictionary<string, string>
        {
            { "client_id", _clientID },
            { "scope", "identify" },
            { "response_type", "code" },
            { "redirect_uri", publicBase + "callback" }
        };

        var url = QueryHelpers.AddQueryString(DiscordAuthorizeUrl, par);
        return url;
    }

    public GenericProviderUserData HandleOAuthCallback(string code, string redirectUri)
    {
        var data = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code! },
            { "redirect_uri", redirectUri },
            { "scope", "identify" }
        };

        var client = new HttpClient();


        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientID}:{client_secret}")));

        var d = new FormUrlEncodedContent(data);
        var response = client.PostAsync(DiscordTokenUrl, d).Result;
        var tokenData = DiscordAuthenticationFlow.ParseTokenDataFromResponse(response);
        if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
        {
            throw new ValidationFailedException("invalid token data returned from discord");
        }
        
        // Get the user's data from the @me endpoint
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
        response = client.GetAsync(DiscordUserUrl).Result;
        
        var userData = DiscordUserData.ParseDiscordUserDataFromResponse(response);
        if (userData == null)
        {
            throw new ValidationFailedException("invalid user data from discord");
        }

        return new GenericProviderUserData()
        {
            ProviderName = "discord",
            ProviderUsername = userData.Username,
            ProviderAccountId = userData.Id
        };


    }
}