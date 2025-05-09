using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OrchardCore.Security;
using OrchardCore.Security.Services;

namespace OrchardCore.Roles.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<IRole> _roleManager;
    private readonly ISystemRoleProvider _systemRoleProvider;

    public RoleService(
        RoleManager<IRole> roleManager,
        ISystemRoleProvider systemRoleProvider)
    {
        _roleManager = roleManager;
        _systemRoleProvider = systemRoleProvider;
    }

    public async Task<IEnumerable<Claim>> GetRoleClaimsAsync(string role, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(role))
        {
            throw new ArgumentException("The role name cannot be null or empty.", nameof(role));
        }

        var entity = await _roleManager.FindByNameAsync(role);
        if (entity == null)
        {
            return Array.Empty<Claim>();
        }

        return await _roleManager.GetClaimsAsync(entity);
    }

    public Task<IEnumerable<IRole>> GetRolesAsync()
    {
        return Task.FromResult<IEnumerable<IRole>>(_roleManager.Roles);
    }

    [Obsolete("This method is deprecated and will be removed in a future version. Use GetRolesAsync() instead.")]
    public Task<IEnumerable<string>> GetRoleNamesAsync()
        => RoleServiceExtensions.GetRoleNamesAsync(this);

    public Task<IEnumerable<string>> GetNormalizedRoleNamesAsync()
    {
        return Task.FromResult<IEnumerable<string>>(_roleManager.Roles.Select(a => _roleManager.NormalizeKey(a.RoleName)));
    }

    public ValueTask<bool> IsAdminRoleAsync(string role)
        => ValueTask.FromResult(_systemRoleProvider.IsAdminRole(role));

    public ValueTask<bool> IsSystemRoleAsync(string role)
         => ValueTask.FromResult(_systemRoleProvider.IsSystemRole(role));
}
