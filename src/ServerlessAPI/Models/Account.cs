using System.Text.Json.Serialization;

namespace Models;

/// <summary>
/// Account represents a user account
/// </summary>
/// <param name="Id">the id of the user account</param>
/// <param name="Username">the username of the user</param>
public class Account(string Id, string Username)
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Id;

    [JsonPropertyName("username")]
    public string Username { get; set; } = Username;
}