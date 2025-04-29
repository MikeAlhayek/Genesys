using System.Security.Claims;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using static OpenIddict.Client.OpenIddictClientHandlerFilters;
using static OpenIddict.Client.OpenIddictClientHandlers;

namespace CloudSolutions.Genesys.Services;

public sealed class MapGenesysWebServicesFederationClaims : IOpenIddictClientHandler<OpenIddictClientEvents.ProcessAuthenticationContext>
{
    /// <summary>
    /// Gets the default descriptor definition assigned to this handler.
    /// </summary>
    public static OpenIddictClientHandlerDescriptor Descriptor { get; }
        = OpenIddictClientHandlerDescriptor.CreateBuilder<OpenIddictClientEvents.ProcessAuthenticationContext>()
            .AddFilter<RequireWebServicesFederationClaimMappingEnabled>()
            .UseSingletonHandler<MapGenesysWebServicesFederationClaims>()
            .SetOrder(MapStandardWebServicesFederationClaims.Descriptor.Order + 1_000)
            .SetType(OpenIddictClientHandlerType.Custom)
            .Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(OpenIddictClientEvents.ProcessAuthenticationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Registration.ProviderName == GenesysConstants.ProviderName)
        {
            context.MergedPrincipal.SetClaim(ClaimTypes.NameIdentifier, context.MergedPrincipal.GetClaim("id"));
            context.MergedPrincipal.SetClaim(ClaimTypes.Name, context.MergedPrincipal.GetClaim("name"));
            context.MergedPrincipal.SetClaim(ClaimTypes.Email, context.MergedPrincipal.GetClaim("email"));
            context.MergedPrincipal.SetClaim(ClaimTypes.Uri, context.MergedPrincipal.GetClaim("selfUri"));
        }

        return default;
    }
}
