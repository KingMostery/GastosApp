using System.ComponentModel.DataAnnotations;
using GastosApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace GastosApp.Pages;

[Authorize]
public class GastosModel : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public GastosModel(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public EditInputModel EditInput { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? FiltroDesde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FiltroHasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FiltroMetodoPago { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FiltroCategoria { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BusquedaDescripcion { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EditarId { get; set; }

    public ApplicationUser UsuarioActual { get; private set; } = null!;

    public List<Gasto> GastosFiltrados { get; private set; } = [];

    public int TotalMovimientosFiltrados { get; private set; }

    public decimal TotalFiltrado { get; private set; }

    public IEnumerable<SelectListItem> MetodoPagoOptions => Enum
        .GetValues<MetodoPago>()
        .Select(value => new SelectListItem(value.ToDisplayName(), value.ToString()));

    public IEnumerable<SelectListItem> CategoriaOptions => Enum
        .GetValues<CategoriaGasto>()
        .Select(value => new SelectListItem(value.ToDisplayName(), value.ToString()));

    public IEnumerable<SelectListItem> MetodoPagoFilterOptions => new[]
        {
            new SelectListItem("Todos", string.Empty)
        }
        .Concat(MetodoPagoOptions);

    public IEnumerable<SelectListItem> CategoriaFilterOptions => new[]
        {
            new SelectListItem("Todas", string.Empty)
        }
        .Concat(CategoriaOptions);

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await CargarUsuarioAsync();
        if (user is null)
        {
            return Challenge();
        }

        await CargarPantallaAsync(user.Id);
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
            await CargarPantallaAsync(user.Id);
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

        TempData["GastosMessage"] = "Gasto registrado correctamente.";
        return RedirectToPage(ConstruirFiltroRouteValues());
    }

    public async Task<IActionResult> OnPostEditarAsync(int id)
    {
        var user = await CargarUsuarioAsync();
        if (user is null)
        {
            return Challenge();
        }

        EditarId = id;
        await CargarPantallaAsync(user.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostActualizarAsync(int id)
    {
        var user = await CargarUsuarioAsync();
        if (user is null)
        {
            return Challenge();
        }

        if (EditInput.Valor <= 0)
        {
            ModelState.AddModelError(nameof(EditInput.Valor), "Ingresa un valor mayor a cero.");
        }

        if (!ModelState.IsValid)
        {
            EditarId = id;
            await CargarPantallaAsync(user.Id);
            return Page();
        }

        var gasto = await _dbContext.Gastos
            .FirstOrDefaultAsync(item => item.Id == id && item.UsuarioId == user.Id);

        if (gasto is null)
        {
            TempData["GastosMessage"] = "No se encontro el gasto que intentas editar.";
            return RedirectToPage(ConstruirFiltroRouteValues());
        }

        gasto.Valor = EditInput.Valor;
        gasto.Fecha = EditInput.Fecha;
        gasto.MetodoPago = Enum.Parse<MetodoPago>(EditInput.MetodoPago);
        gasto.Categoria = Enum.Parse<CategoriaGasto>(EditInput.Categoria);
        gasto.Descripcion = string.IsNullOrWhiteSpace(EditInput.Descripcion)
            ? null
            : EditInput.Descripcion.Trim();

        await _dbContext.SaveChangesAsync();

        TempData["GastosMessage"] = "Gasto actualizado correctamente.";
        return RedirectToPage(ConstruirFiltroRouteValues());
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var user = await CargarUsuarioAsync();
        if (user is null)
        {
            return Challenge();
        }

        var gasto = await _dbContext.Gastos
            .FirstOrDefaultAsync(item => item.Id == id && item.UsuarioId == user.Id);

        if (gasto is null)
        {
            TempData["GastosMessage"] = "No se encontro el gasto que intentas eliminar.";
            return RedirectToPage(ConstruirFiltroRouteValues());
        }

        _dbContext.Gastos.Remove(gasto);
        await _dbContext.SaveChangesAsync();

        TempData["GastosMessage"] = "Gasto eliminado correctamente.";
        return RedirectToPage(ConstruirFiltroRouteValues());
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

    private async Task CargarPantallaAsync(string userId)
    {
        var gastosUsuario = _dbContext.Gastos
            .AsNoTracking()
            .Where(gasto => gasto.UsuarioId == userId);

        var gastosFiltradosQuery = AplicarFiltros(gastosUsuario);
        GastosFiltrados = await gastosFiltradosQuery
            .OrderByDescending(gasto => gasto.Fecha)
            .ThenByDescending(gasto => gasto.Id)
            .Take(100)
            .ToListAsync();

        TotalMovimientosFiltrados = GastosFiltrados.Count;
        TotalFiltrado = GastosFiltrados.Sum(gasto => gasto.Valor);

        await CargarEdicionAsync(userId);
    }

    private IQueryable<Gasto> AplicarFiltros(IQueryable<Gasto> query)
    {
        if (FiltroDesde.HasValue)
        {
            var desde = FiltroDesde.Value.Date;
            query = query.Where(gasto => gasto.Fecha >= desde);
        }

        if (FiltroHasta.HasValue)
        {
            var hasta = FiltroHasta.Value.Date.AddDays(1);
            query = query.Where(gasto => gasto.Fecha < hasta);
        }

        if (Enum.TryParse<MetodoPago>(FiltroMetodoPago, out var metodoPago))
        {
            query = query.Where(gasto => gasto.MetodoPago == metodoPago);
        }

        if (Enum.TryParse<CategoriaGasto>(FiltroCategoria, out var categoria))
        {
            query = query.Where(gasto => gasto.Categoria == categoria);
        }

        if (!string.IsNullOrWhiteSpace(BusquedaDescripcion))
        {
            var texto = BusquedaDescripcion.Trim().ToLower();
            query = query.Where(gasto => (gasto.Descripcion ?? string.Empty).ToLower().Contains(texto));
        }

        return query;
    }

    private async Task CargarEdicionAsync(string userId)
    {
        if (!EditarId.HasValue)
        {
            return;
        }

        var gasto = await _dbContext.Gastos
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == EditarId.Value && item.UsuarioId == userId);

        if (gasto is null)
        {
            EditarId = null;
            return;
        }

        EditInput = new EditInputModel
        {
            Valor = gasto.Valor,
            Fecha = gasto.Fecha,
            MetodoPago = gasto.MetodoPago.ToString(),
            Categoria = gasto.Categoria.ToString(),
            Descripcion = gasto.Descripcion
        };
    }

    private RouteValueDictionary ConstruirFiltroRouteValues()
    {
        var routeValues = new RouteValueDictionary();

        if (FiltroDesde.HasValue)
        {
            routeValues[nameof(FiltroDesde)] = FiltroDesde.Value.ToString("yyyy-MM-dd");
        }

        if (FiltroHasta.HasValue)
        {
            routeValues[nameof(FiltroHasta)] = FiltroHasta.Value.ToString("yyyy-MM-dd");
        }

        if (!string.IsNullOrWhiteSpace(FiltroMetodoPago))
        {
            routeValues[nameof(FiltroMetodoPago)] = FiltroMetodoPago;
        }

        if (!string.IsNullOrWhiteSpace(FiltroCategoria))
        {
            routeValues[nameof(FiltroCategoria)] = FiltroCategoria;
        }

        if (!string.IsNullOrWhiteSpace(BusquedaDescripcion))
        {
            routeValues[nameof(BusquedaDescripcion)] = BusquedaDescripcion;
        }

        return routeValues;
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

    public class EditInputModel
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
}
