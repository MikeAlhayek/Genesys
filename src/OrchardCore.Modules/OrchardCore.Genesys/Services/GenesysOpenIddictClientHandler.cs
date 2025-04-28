using OpenIddict.Client;
using static OpenIddict.Client.OpenIddictClientEvents;

namespace CloudSolutions.Genesys.Services;

public sealed class GenesysOpenIddictClientHandler : IOpenIddictClientHandler<ApplyRedirectionResponseContext>
{
    public ValueTask HandleAsync(ApplyRedirectionResponseContext context)
    {
        throw new NotImplementedException();
    }
}
