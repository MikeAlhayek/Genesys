using CloudSolutions.Genesys.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PureCloudPlatform.Client.V2.Api;
using PureCloudPlatform.Client.V2.Client;
using PureCloudPlatform.Client.V2.Extensions;
using PureCloudPlatform.Client.V2.Model;

namespace CloudSolutions.Genesys.Services;

public sealed class GenesysNotificationChannelService
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
    private readonly GenesysAuthenticationOptions _genesysOptions;
    private readonly ILogger _logger;

    private bool _disposed;
    private Channel _channel = null;

    public GenesysNotificationChannelService(
        IOptions<GenesysAuthenticationOptions> genesysOptions,
        ILogger<GenesysNotificationChannelService> logger)
    {
        _genesysOptions = genesysOptions.Value;
        _logger = logger;
    }

    public async Task<Channel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        if (_channel is not null)
        {
            return _channel;
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_channel is not null)
            {
                return _channel;
            }

            var region = Enum.Parse<PureCloudRegionHosts>("us_west_2");

            var client = new ApiClient();
            client.setBasePath(region);

            var accessTokenInfo = client.PostToken(_genesysOptions.ClientId, _genesysOptions.ClientSecret);

            // var authenticationResult = await _clientService.AuthenticateWithClientCredentialsAsync(new()
            // {
            //     ProviderName = "Genesys",
            // });
            // 
            // authenticationResult.AccessToken;

            var apiInstance = new NotificationsApi(new Configuration
            {
                ApiClient = client,
                AccessToken = accessTokenInfo.AccessToken,
            });

            // Create the WebSocket notification channel.
            _channel = await apiInstance.PostNotificationsChannelsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something failed during the communication with Genesys channel.");
        }
        finally
        {
            _semaphore.Release();
        }

        return _channel;
    }

    public void Dispose()
    {
        Dispose(true);
    }

    public void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _disposed = true;

            _channel = null;
        }
    }
}
