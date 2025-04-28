using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using OrchardCore.OpenId.ViewModels;

namespace CloudSolutions.Genesys.ViewModels;

public class GenesysSettingsViewModel : OpenIdClientSettingsViewModel
{
    [BindNever]
    public bool HasSecret { get; set; }

    [BindNever]
    public IEnumerable<SelectListItem> Authorities { get; set; }
}

