using CloudSolutions.Genesys.Models;
using CloudSolutions.Genesys.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Environment.Shell;
using OrchardCore.Mvc.ModelBinding;
using OrchardCore.Settings;
using PureCloudPlatform.Client.V2.Client;

namespace CloudSolutions.Genesys.Drivers;

internal sealed class GenesysSettingsDisplayDriver : SiteDisplayDriver<GenesysSettings>
{
    public const string GenesysProtectorName = "GenesysSettings";

    public const string GroupId = "genesysSettings";

    protected override string SettingsGroupId => GroupId;

    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellReleaseManager _shellReleaseManager;

    internal readonly IStringLocalizer S;

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

    public override IDisplayResult Edit(ISite site, GenesysSettings settings, BuildEditorContext context)
    {
        return Initialize<GenesysSettingsViewModel>("GenesysSettings_Edit", model =>
        {
            model.Organization = settings.Organization;
            model.Region = settings.Region;
            model.ClientId = settings.ClientId;
            model.HasSecret = !string.IsNullOrWhiteSpace(settings.ClientSecret);
        }).Location("Content:5")
        .RenderWhen(() => _authorizationService.AuthorizeAsync(_httpContextAccessor.HttpContext?.User, GenesysPermissions.ManageGenesysIntegration))
        .OnGroup(SettingsGroupId);
    }

    public override async Task<IDisplayResult> UpdateAsync(ISite site, GenesysSettings settings, UpdateEditorContext context)
    {
        if (!await _authorizationService.AuthorizeAsync(_httpContextAccessor.HttpContext?.User, GenesysPermissions.ManageGenesysIntegration))
        {
            return null;
        }

        var model = new GenesysSettingsViewModel();

        await context.Updater.TryUpdateModelAsync(model, Prefix);

        if (string.IsNullOrEmpty(model.Organization))
        {
            context.Updater.ModelState.AddModelError(Prefix, nameof(model.Organization), S["The Organization is required."]);
        }

        if (string.IsNullOrEmpty(model.Region))
        {
            context.Updater.ModelState.AddModelError(Prefix, nameof(model.Region), S["The Region is required."]);
        }
        else if (!Enum.TryParse<PureCloudRegionHosts>(model.Region, out _))
        {
            context.Updater.ModelState.AddModelError(Prefix, nameof(model.Region), S["Invalid Region value."]);
        }

        if (string.IsNullOrEmpty(model.ClientId))
        {
            context.Updater.ModelState.AddModelError(Prefix, nameof(model.ClientId), S["The Client Id is required."]);
        }

        var hasChange = settings.Organization != model.Organization || settings.ClientId != model.ClientId;

        var hasNewSecret = !string.IsNullOrEmpty(model.ClientSecret);

        if (context.IsNew && !hasNewSecret)
        {
            context.Updater.ModelState.AddModelError(Prefix, nameof(model.ClientSecret), S["The Client Secret is required."]);
        }
        else if (hasNewSecret)
        {
            var protector = _dataProtectionProvider.CreateProtector(GenesysProtectorName);

            settings.ClientSecret = protector.Protect(model.ClientSecret);
            hasChange = true;
        }

        settings.Organization = model.Organization;
        settings.Region = model.Region;
        settings.ClientId = model.ClientId;

        if (hasChange)
        {
            _shellReleaseManager.RequestRelease();
        }

        return Edit(site, settings, context);
    }
}
