using System.Text.Json;
using System.Text.Json.Serialization;
using ThirdParty.Json.LitJson;

namespace Models
{
    

/// <summary>
/// Token data class represents the data returned from the Discord API when exchanging the code for a token.
/// This class is used to deserialize the JSON response from the Discord API.
/// </summary>
internal class DiscordAuthenticationFlow
{
    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; init; }
    [JsonPropertyName("scope")]
    public required string Scope { get; init; }
    
    
    public static DiscordAuthenticationFlow? ParseTokenDataFromResponse(HttpResponseMessage? response)
    {
        var data = response.Content.ReadAsStringAsync().Result;

        if (string.IsNullOrEmpty(data))
        {
            return null;
        }
        
        try {
            return JsonSerializer.Deserialize<DiscordAuthenticationFlow>(data);
        }catch(Exception e){
            return null;
        }
    }
};


/// <summary>
/// DiscordUserData class represents the data returned from the Discord API when requesting the user's data.
/// This class is used to deserialize the JSON response from the Discord API.
/// </summary>
internal class DiscordUserData
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    [JsonPropertyName("username")]
    public required string Username { get; init; }
    [JsonPropertyName("avatar")]
    public required string Avatar { get; init; }
    [JsonPropertyName("discriminator")]
    public required string Discriminator { get; init; }
    [JsonPropertyName("public_flags")]
    public int PublicFlags { get; init; }
    [JsonPropertyName("flags")]
    public int Flags { get; init; }
    [JsonPropertyName("banner")]
    public required string Banner { get; init; }
    [JsonPropertyName("accent_color")]
    public required string AccentColor { get; init; }
    [JsonPropertyName("global_name")]
    public required string GlobalName { get; init; }
    [JsonPropertyName("avatar_decoration_data")]
    public required string AvatarDecorationData { get; init; }
    [JsonPropertyName("banner_color")]
    public required string BannerColor { get; init; }
    [JsonPropertyName("clan")]
    public required string Clan { get; init; }
    [JsonPropertyName("mfa_enabled")]
    public bool MfaEnabled { get; init; }
    [JsonPropertyName("locale")]
    public required string Locale { get; init; }
    [JsonPropertyName("premium_type")]
    public int PremiumType { get; init; }
    [JsonPropertyName("email")]
    public required string Email { get; init; }
    [JsonPropertyName("verified")]
    public bool Verified { get; init; }

    public static DiscordUserData? ParseDiscordUserDataFromResponse(HttpResponseMessage? response)
    {
        var responseContent = response.Content.ReadAsStringAsync().Result;
        
        try {
            return JsonSerializer.Deserialize<DiscordUserData>(responseContent);
        }catch(Exception e){
                return null;
        }
    }
}
}
