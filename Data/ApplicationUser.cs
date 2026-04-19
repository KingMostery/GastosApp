using Microsoft.AspNetCore.Identity;

namespace GastosApp.Data;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
}
