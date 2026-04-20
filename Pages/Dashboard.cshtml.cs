using System.ComponentModel.DataAnnotations;
using GastosApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public ApplicationUser UsuarioActual { get; private set; } = null!;

    public decimal TotalMesActual { get; private set; }

    public decimal PromedioMesActual { get; private set; }

    public int TotalMovimientosMesActual { get; private set; }

    public List<Gasto> GastosRecientes { get; private set; } = [];

    public List<CategoriaResumen> ResumenPorCategoria { get; private set; } = [];

    public IEnumerable<SelectListItem> MetodoPagoOptions => Enum
        .GetValues<MetodoPago>()
        .Select(value => new SelectListItem(value.ToDisplayName(), value.ToString()));

    public IEnumerable<SelectListItem> CategoriaOptions => Enum
        .GetValues<CategoriaGasto>()
        .Select(value => new SelectListItem(value.ToDisplayName(), value.ToString()));

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await CargarUsuarioAsync();
        if (user is null)
        {
            return Challenge();
        }

        await CargarDashboardAsync(user.Id);
        Input.Fecha = DateTime.Today;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await CargarUsuarioAsync();
        if (user is null)
        {
            return Challenge();
        }

        if (Input.Valor <= 0)
        {
            ModelState.AddModelError(nameof(Input.Valor), "Ingresa un valor mayor a cero.");
        }

        if (!ModelState.IsValid)
        {
            await CargarDashboardAsync(user.Id);
            return Page();
        }

        var gasto = new Gasto
        {
            Valor = Input.Valor,
            Fecha = Input.Fecha,
            MetodoPago = Enum.Parse<MetodoPago>(Input.MetodoPago),
            Categoria = Enum.Parse<CategoriaGasto>(Input.Categoria),
            Descripcion = string.IsNullOrWhiteSpace(Input.Descripcion) ? null : Input.Descripcion.Trim(),
            UsuarioId = user.Id
        };

        _dbContext.Gastos.Add(gasto);
        await _dbContext.SaveChangesAsync();

        TempData["DashboardMessage"] = "Gasto registrado correctamente.";
        return RedirectToPage();
    }

    private async Task<ApplicationUser?> CargarUsuarioAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            UsuarioActual = user;
        }

        return user;
    }

    private async Task CargarDashboardAsync(string userId)
    {
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var siguienteMes = inicioMes.AddMonths(1);

        var gastosUsuario = _dbContext.Gastos
            .AsNoTracking()
            .Where(gasto => gasto.UsuarioId == userId);

        GastosRecientes = await gastosUsuario
            .OrderByDescending(gasto => gasto.Fecha)
            .ThenByDescending(gasto => gasto.Id)
            .Take(8)
            .ToListAsync();

        // SQLite no soporta Sum sobre decimal en SQL; traemos los datos del mes y calculamos en memoria.
        var gastosMesLista = await gastosUsuario
            .Where(gasto => gasto.Fecha >= inicioMes && gasto.Fecha < siguienteMes)
            .ToListAsync();

        TotalMesActual = gastosMesLista.Sum(gasto => gasto.Valor);
        TotalMovimientosMesActual = gastosMesLista.Count;
        PromedioMesActual = TotalMovimientosMesActual == 0 ? 0 : Math.Round(TotalMesActual / TotalMovimientosMesActual, 2);

        ResumenPorCategoria = gastosMesLista
            .GroupBy(gasto => gasto.Categoria)
            .Select(group => new CategoriaResumen
            {
                Categoria = group.Key,
                Total = group.Sum(gasto => gasto.Valor),
                Cantidad = group.Count()
            })
            .OrderByDescending(item => item.Total)
            .ToList();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "El valor del gasto es obligatorio.")]
        [Display(Name = "Valor del gasto")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Selecciona un metodo de pago.")]
        [Display(Name = "Metodo de pago")]
        public string MetodoPago { get; set; } = GastosApp.Data.MetodoPago.TarjetaDebito.ToString();

        [Required(ErrorMessage = "Selecciona una categoria.")]
        [Display(Name = "Categoria")]
        public string Categoria { get; set; } = CategoriaGasto.Alimentacion.ToString();

        [StringLength(160, ErrorMessage = "La descripcion puede tener maximo 160 caracteres.")]
        [Display(Name = "Descripcion")]
        public string? Descripcion { get; set; }
    }

    public class CategoriaResumen
    {
        public CategoriaGasto Categoria { get; set; }

        public decimal Total { get; set; }

        public int Cantidad { get; set; }
    }
}

internal static class GastoEnumExtensions
{
    public static string ToDisplayName(this MetodoPago value) => value switch
    {
        MetodoPago.Efectivo => "Efectivo",
        MetodoPago.TarjetaDebito => "Tarjeta débito",
        MetodoPago.TarjetaCredito => "Tarjeta crédito",
        MetodoPago.Transferencia => "Transferencia",
        MetodoPago.Nequi => "Nequi",
        MetodoPago.Daviplata => "Daviplata",
        _ => "Otro"
    };

    public static string ToDisplayName(this CategoriaGasto value) => value switch
    {
        CategoriaGasto.Alimentacion => "Alimentación",
        CategoriaGasto.Transporte => "Transporte",
        CategoriaGasto.Hogar => "Hogar",
        CategoriaGasto.Salud => "Salud",
        CategoriaGasto.Educacion => "Educación",
        CategoriaGasto.Entretenimiento => "Entretenimiento",
        CategoriaGasto.Servicios => "Servicios",
        CategoriaGasto.Suscripciones => "Suscripciones",
        CategoriaGasto.Compras => "Compras",
        _ => "Otro"
    };
}
