using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace CloudSolutions.Genesys.Models;

public sealed class GenesysAuthenticationOptions : OAuthOptions
{
    public string Organization { get; set; }

    public string Region { get; set; }

    public bool HasCredentials
        => !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret);

    public GenesysAuthenticationOptions()
    {
        ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
        ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
        ClaimActions.MapJsonKey(ClaimTypes.Uri, "url");
    }
}
