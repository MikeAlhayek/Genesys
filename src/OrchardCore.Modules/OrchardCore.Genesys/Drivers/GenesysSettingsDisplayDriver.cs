using System.Text.Json;
using CloudSolutions.Genesys.Models;
using CloudSolutions.Genesys.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Environment.Shell;
using OrchardCore.Mvc.ModelBinding;
using OrchardCore.OpenId;
using OrchardCore.OpenId.Settings;
using OrchardCore.Settings;

namespace CloudSolutions.Genesys.Drivers;

internal sealed class GenesysSettingsDisplayDriver : SiteDisplayDriver<GenesysSettings>
{
    public const string GenesysProtectorName = "GenesysSettings";

    public const string GroupId = "genesysSettings";

    private static readonly char[] _separator = [' ', ','];

    private readonly IAuthorizationService _authorizationService;
    private readonly IShellReleaseManager _shellReleaseManager;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    internal readonly IStringLocalizer S;

    protected override string SettingsGroupId => GroupId;

    public GenesysSettingsDisplayDriver(
        IDataProtectionProvider dataProtectionProvider,
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService,
        IShellReleaseManager shellReleaseManager,
        IStringLocalizer<GenesysSettingsDisplayDriver> stringLocalizer)
    {
        _dataProtectionProvider = dataProtectionProvider;
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
        _shellReleaseManager = shellReleaseManager;
        S = stringLocalizer;
    }

    public override async Task<IDisplayResult> EditAsync(ISite site, GenesysSettings settings, BuildEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (!await _authorizationService.AuthorizeAsync(user, OpenIdPermissions.ManageClientSettings))
        {
            return null;
        }

        context.AddTenantReloadWarningWrapper();

        return Initialize<GenesysSettingsViewModel>("GenesysSettings_Edit", model =>
        {
            model.ClientId = settings.ClientId;
            model.HasSecret = !string.IsNullOrWhiteSpace(settings.ClientSecret);
            model.DisplayName = settings.DisplayName;
            model.Scopes = settings.Scopes != null ? string.Join(' ', settings.Scopes) : null;
            model.Authority = settings.Authority?.ToString();
            model.CallbackPath = settings.CallbackPath;
            model.ClientId = settings.ClientId;
            model.HasClientSecret = !string.IsNullOrEmpty(settings.ClientSecret);
            model.SignedOutCallbackPath = settings.SignedOutCallbackPath;
            model.SignedOutRedirectUri = settings.SignedOutRedirectUri;
            model.ResponseMode = settings.ResponseMode;
            model.StoreExternalTokens = settings.StoreExternalTokens;
            model.Authorities = new List<SelectListItem>()
            {
                new SelectListItem("us_east_1 (mypurecloud.com)", "https://mypurecloud.com/"),
                new SelectListItem("eu_west_1 (mypurecloud.ie)", "https://mypurecloud.ie/"),
                new SelectListItem("eu_central_1 (mypurecloud.de)", "https://mypurecloud.de/"),
                new SelectListItem("ap_northeast_1 (mypurecloud.jp)", "https://mypurecloud.jp/"),
                new SelectListItem("ap_southeast_2 (mypurecloud.com.au)", "https://mypurecloud.com.au/"),
                new SelectListItem("us_west_2 (usw2.pure.cloud)", "https://usw2.pure.cloud/"),
                new SelectListItem("ca_central_1 (cac1.pure.cloud)", "https://cac1.pure.cloud/"),
                new SelectListItem("ap_northeast_2 (apne2.pure.cloud)", "https://apne2.pure.cloud/"),
                new SelectListItem("eu_west_2 (mypurecloud.com)", "https://euw2.pure.cloud/"),
                new SelectListItem("ap_south_1 (use2.us-gov-pure.cloud)", "https://use2.us-gov-pure.cloud/"),
                new SelectListItem("sa_east_1 (sae1.pure.cloud)", "https://sae1.pure.cloud/"),
                new SelectListItem("me_central_1 (mypurecloud.com)", "https://mec1.pure.cloud/"),
                new SelectListItem("ap_northeast_3 (apne3.pure.cloud)", "https://apne3.pure.cloud/"),
                new SelectListItem("eu_central_2 (euc2.pure.cloud)", "https://euc2.pure.cloud/"),
            };

            if (settings.ResponseType == OpenIdConnectResponseType.Code)
            {
                model.UseCodeFlow = true;
            }
            else if (settings.ResponseType == OpenIdConnectResponseType.CodeIdToken)
            {
                model.UseCodeIdTokenFlow = true;
            }
            else if (settings.ResponseType == OpenIdConnectResponseType.CodeIdTokenToken)
            {
                model.UseCodeIdTokenTokenFlow = true;
            }
            else if (settings.ResponseType == OpenIdConnectResponseType.CodeToken)
            {
                model.UseCodeTokenFlow = true;
            }
            else if (settings.ResponseType == OpenIdConnectResponseType.IdToken)
            {
                model.UseIdTokenFlow = true;
            }
            else if (settings.ResponseType == OpenIdConnectResponseType.IdTokenToken)
            {
                model.UseIdTokenTokenFlow = true;
            }

            model.Parameters = JConvert.SerializeObject(settings.Parameters, JOptions.CamelCase);
        }).Location("Content:5")
        .OnGroup(SettingsGroupId);
    }

    public override async Task<IDisplayResult> UpdateAsync(ISite site, GenesysSettings settings, UpdateEditorContext context)
    {
        if (!await _authorizationService.AuthorizeAsync(_httpContextAccessor.HttpContext?.User, GenesysPermissions.ManageGenesysIntegration))
        {
            return null;
        }

        var previousClientSecret = settings.ClientSecret;

        var model = new GenesysSettingsViewModel();

        await context.Updater.TryUpdateModelAsync(model, Prefix);

        if (string.IsNullOrEmpty(model.ClientId))
        {
            context.Updater.ModelState.AddModelError(Prefix, nameof(model.ClientId), S["The Client Id is required."]);
        }

        var hasNewSecret = !string.IsNullOrEmpty(model.ClientSecret);

        if (context.IsNew && !hasNewSecret)
        {
            context.Updater.ModelState.AddModelError(Prefix, nameof(model.ClientSecret), S["The Client Secret is required."]);
        }
        else if (hasNewSecret)
        {
            var protector = _dataProtectionProvider.CreateProtector(GenesysProtectorName);

            settings.ClientSecret = protector.Protect(model.ClientSecret);
        }

        settings.ClientId = model.ClientId;

        model.Scopes ??= string.Empty;

        settings.DisplayName = model.DisplayName;
        settings.Scopes = model.Scopes.Split(_separator, StringSplitOptions.RemoveEmptyEntries);

        if (string.IsNullOrEmpty(model.Authority))
        {
            context.Updater.ModelState.AddModelError(Prefix, nameof(model.Authority), S["The Authority is required."]);
        }
        else if (!Uri.TryCreate(model.Authority, UriKind.Absolute, out var authority))
        {
            context.Updater.ModelState.AddModelError(Prefix, nameof(model.Authority), S["Invalid Authority value."]);
        }
        else
        {
            settings.Authority = authority;
        }

        settings.CallbackPath = model.CallbackPath;
        settings.ClientId = model.ClientId;
        settings.SignedOutCallbackPath = model.SignedOutCallbackPath;
        settings.SignedOutRedirectUri = model.SignedOutRedirectUri;
        settings.ResponseMode = model.ResponseMode;
        settings.StoreExternalTokens = model.StoreExternalTokens;

        var useClientSecret = true;

        if (model.UseCodeFlow)
        {
            settings.ResponseType = OpenIdConnectResponseType.Code;
        }
        else if (model.UseCodeIdTokenFlow)
        {
            settings.ResponseType = OpenIdConnectResponseType.CodeIdToken;
        }
        else if (model.UseCodeIdTokenTokenFlow)
        {
            settings.ResponseType = OpenIdConnectResponseType.CodeIdTokenToken;
        }
        else if (model.UseCodeTokenFlow)
        {
            settings.ResponseType = OpenIdConnectResponseType.CodeToken;
        }
        else if (model.UseIdTokenFlow)
        {
            settings.ResponseType = OpenIdConnectResponseType.IdToken;
            useClientSecret = false;
        }
        else if (model.UseIdTokenTokenFlow)
        {
            settings.ResponseType = OpenIdConnectResponseType.IdTokenToken;
            useClientSecret = false;
        }
        else
        {
            settings.ResponseType = OpenIdConnectResponseType.None;
            useClientSecret = false;
        }

        try
        {
            settings.Parameters = string.IsNullOrWhiteSpace(model.Parameters)
                ? []
                : JConvert.DeserializeObject<ParameterSetting[]>(model.Parameters);
        }
        catch
        {
            context.Updater.ModelState.AddModelError(Prefix, S["The parameters are written in an incorrect format."]);
        }

        if (!useClientSecret)
        {
            model.ClientSecret = previousClientSecret = null;
        }

        if (!string.IsNullOrEmpty(model.ClientSecret))
        {
            var protector = _dataProtectionProvider.CreateProtector(GenesysProtectorName);
            settings.ClientSecret = protector.Protect(model.ClientSecret);
        }

        _shellReleaseManager.RequestRelease();

        return await EditAsync(site, settings, context);
    }
}
