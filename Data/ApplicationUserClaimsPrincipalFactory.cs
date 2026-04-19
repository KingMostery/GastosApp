using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GastosApp.Data;

public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var fullName = string.IsNullOrWhiteSpace(user.NombreCompleto) ? user.UserName : user.NombreCompleto;

        // Reemplaza el claim Name para que toda la app muestre el nombre completo.
        var existingNameClaim = identity.FindFirst(ClaimTypes.Name);
        if (existingNameClaim != null)
        {
            identity.RemoveClaim(existingNameClaim);
        }

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, fullName));
        }

        return identity;
    }
}
