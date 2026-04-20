using GastosApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GastosApp.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public ApplicationUser UsuarioActual { get; private set; } = null!;

    public decimal TotalUltimos30Dias { get; private set; }

    public decimal TotalUltimos15Dias { get; private set; }

    public int MovimientosUltimos30Dias { get; private set; }

    public int MovimientosUltimos15Dias { get; private set; }

    public List<Gasto> GastosRecientes { get; private set; } = [];

    public List<CategoriaResumen> ResumenPorCategoria { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        UsuarioActual = user;
        await CargarDashboardAsync(user.Id);
        return Page();
    }

    private async Task CargarDashboardAsync(string userId)
    {
        var hoy = DateTime.Today;
        var desde30 = hoy.AddDays(-30);
        var desde15 = hoy.AddDays(-15);

        var gastosUsuario = await _dbContext.Gastos
            .AsNoTracking()
            .Where(gasto => gasto.UsuarioId == userId)
            .OrderByDescending(gasto => gasto.Fecha)
            .ThenByDescending(gasto => gasto.Id)
            .ToListAsync();

        var gastos30 = gastosUsuario
            .Where(gasto => gasto.Fecha.Date >= desde30)
            .ToList();

        var gastos15 = gastosUsuario
            .Where(gasto => gasto.Fecha.Date >= desde15)
            .ToList();

        TotalUltimos30Dias = gastos30.Sum(gasto => gasto.Valor);
        TotalUltimos15Dias = gastos15.Sum(gasto => gasto.Valor);
        MovimientosUltimos30Dias = gastos30.Count;
        MovimientosUltimos15Dias = gastos15.Count;

        GastosRecientes = gastos30
            .Take(6)
            .ToList();

        ResumenPorCategoria = gastos30
            .GroupBy(gasto => gasto.Categoria)
            .Select(group => new CategoriaResumen
            {
                Categoria = group.Key,
                Total = group.Sum(gasto => gasto.Valor),
                Cantidad = group.Count()
            })
            .OrderByDescending(item => item.Total)
            .Take(5)
            .ToList();
    }

    public class CategoriaResumen
    {
        public CategoriaGasto Categoria { get; set; }

        public decimal Total { get; set; }

        public int Cantidad { get; set; }
    }
}
