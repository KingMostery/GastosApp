using GastosApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GastosApp.Pages;

[Authorize]
public class IngresosModel : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public IngresosModel(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    // ── Listas ───────────────────────────────────────────────────────────────
    public List<Ingreso> Ingresos { get; private set; } = [];

    // ── Totales ──────────────────────────────────────────────────────────────
    public decimal TotalMes { get; private set; }
    public decimal TotalGeneral { get; private set; }

    // ── Formulario nuevo ─────────────────────────────────────────────────────
    [BindProperty]
    public InputModel Input { get; set; } = new();

    // ── Edición inline ───────────────────────────────────────────────────────
    [BindProperty]
    public EditInputModel EditInput { get; set; } = new();

    // ── GET ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        await CargarAsync(user.Id);
        return Page();
    }

    // ── POST: nuevo ingreso ───────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        // Limpiar errores del formulario de edición que no aplican aquí
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("EditInput")).ToList())
            ModelState.Remove(key);

        if (Input.Valor <= 0)
            ModelState.AddModelError("Input.Valor", "El valor debe ser mayor que cero.");

        if (!ModelState.IsValid)
        {
            await CargarAsync(user.Id);
            return Page();
        }

        _dbContext.Ingresos.Add(new Ingreso
        {
            Valor = Input.Valor,
            Fecha = Input.Fecha,
            Concepto = Input.Concepto.Trim(),
            Fuente = Input.Fuente,
            Descripcion = string.IsNullOrWhiteSpace(Input.Descripcion) ? null : Input.Descripcion.Trim(),
            UsuarioId = user.Id
        });

        await _dbContext.SaveChangesAsync();
        return RedirectToPage();
    }

    // ── POST: eliminar ────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var ingreso = await _dbContext.Ingresos
            .FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == user.Id);

        if (ingreso is not null)
        {
            _dbContext.Ingresos.Remove(ingreso);
            await _dbContext.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    // ── POST: cargar datos para editar ────────────────────────────────────────
    public async Task<IActionResult> OnPostEditarAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var ingreso = await _dbContext.Ingresos
            .FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == user.Id);

        if (ingreso is null) return RedirectToPage();

        EditInput = new EditInputModel
        {
            Id = ingreso.Id,
            Valor = ingreso.Valor,
            Fecha = ingreso.Fecha,
            Concepto = ingreso.Concepto,
            Fuente = ingreso.Fuente,
            Descripcion = ingreso.Descripcion
        };

        await CargarAsync(user.Id);
        return Page();
    }

    // ── POST: guardar edición ─────────────────────────────────────────────────
    public async Task<IActionResult> OnPostActualizarAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        // Limpiar errores del formulario nuevo que no aplican aquí
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Input.")).ToList())
            ModelState.Remove(key);

        if (EditInput.Valor <= 0)
            ModelState.AddModelError("EditInput.Valor", "El valor debe ser mayor que cero.");

        if (!ModelState.IsValid)
        {
            await CargarAsync(user.Id);
            return Page();
        }

        var ingreso = await _dbContext.Ingresos
            .FirstOrDefaultAsync(i => i.Id == EditInput.Id && i.UsuarioId == user.Id);

        if (ingreso is not null)
        {
            ingreso.Valor = EditInput.Valor;
            ingreso.Fecha = EditInput.Fecha;
            ingreso.Concepto = EditInput.Concepto.Trim();
            ingreso.Fuente = EditInput.Fuente;
            ingreso.Descripcion = string.IsNullOrWhiteSpace(EditInput.Descripcion) ? null : EditInput.Descripcion.Trim();
            await _dbContext.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task CargarAsync(string userId)
    {
        var todos = await _dbContext.Ingresos
            .AsNoTracking()
            .Where(i => i.UsuarioId == userId)
            .OrderByDescending(i => i.Fecha)
            .ThenByDescending(i => i.Id)
            .ToListAsync();

        Ingresos = todos;
        TotalGeneral = todos.Sum(i => i.Valor);

        var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        TotalMes = todos.Where(i => i.Fecha >= inicioMes).Sum(i => i.Valor);
    }

    // ── View models ───────────────────────────────────────────────────────────
    public class InputModel
    {
        [Required(ErrorMessage = "El valor es obligatorio.")]
        public decimal Valor { get; set; }

        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "El concepto es obligatorio.")]
        [StringLength(120, ErrorMessage = "Máximo 120 caracteres.")]
        public string Concepto { get; set; } = string.Empty;

        public FuenteIngreso Fuente { get; set; } = FuenteIngreso.Salario;

        [StringLength(200)]
        public string? Descripcion { get; set; }
    }

    public class EditInputModel
    {
        public int Id { get; set; }

        public decimal Valor { get; set; }

        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [StringLength(120, ErrorMessage = "Máximo 120 caracteres.")]
        public string Concepto { get; set; } = string.Empty;

        public FuenteIngreso Fuente { get; set; } = FuenteIngreso.Salario;

        [StringLength(200)]
        public string? Descripcion { get; set; }
    }
}
