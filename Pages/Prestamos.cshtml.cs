using GastosApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GastosApp.Pages;

[Authorize]
public class PrestamosModel : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public PrestamosModel(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    // ── Listas ───────────────────────────────────────────────────────────────
    public List<Prestamo> Prestamos { get; private set; } = [];
    public List<Prestamo> PrestamosPendientes { get; private set; } = [];

    public decimal TotalPendiente { get; private set; }

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

    // ── POST: nuevo préstamo ──────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        // Limpiar errores del formulario de edición que no aplican aquí
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("EditInput")).ToList())
            ModelState.Remove(key);

        if (Input.Monto <= 0)
            ModelState.AddModelError("Input.Monto", "El monto debe ser mayor que cero.");

        if (!ModelState.IsValid)
        {
            await CargarAsync(user.Id);
            return Page();
        }

        _dbContext.Prestamos.Add(new Prestamo
        {
            Monto = Input.Monto,
            Persona = Input.Persona.Trim(),
            FechaPrestamo = Input.FechaPrestamo,
            FechaEstimadaDevolucion = Input.FechaEstimadaDevolucion,
            Estado = EstadoPrestamo.Pendiente,
            Notas = string.IsNullOrWhiteSpace(Input.Notas) ? null : Input.Notas.Trim(),
            UsuarioId = user.Id
        });

        await _dbContext.SaveChangesAsync();
        return RedirectToPage();
    }

    // ── POST: marcar como devuelto ────────────────────────────────────────────
    public async Task<IActionResult> OnPostMarcarDevueltoAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var prestamo = await _dbContext.Prestamos
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == user.Id);

        if (prestamo is not null)
        {
            prestamo.Estado = EstadoPrestamo.Devuelto;
            await _dbContext.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    // ── POST: eliminar ────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var prestamo = await _dbContext.Prestamos
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == user.Id);

        if (prestamo is not null)
        {
            _dbContext.Prestamos.Remove(prestamo);
            await _dbContext.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    // ── POST: cargar datos para editar ────────────────────────────────────────
    public async Task<IActionResult> OnPostEditarAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var prestamo = await _dbContext.Prestamos
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == user.Id);

        if (prestamo is null) return RedirectToPage();

        EditInput = new EditInputModel
        {
            Id = prestamo.Id,
            Monto = prestamo.Monto,
            Persona = prestamo.Persona,
            FechaPrestamo = prestamo.FechaPrestamo,
            FechaEstimadaDevolucion = prestamo.FechaEstimadaDevolucion,
            Notas = prestamo.Notas
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

        if (EditInput.Monto <= 0)
            ModelState.AddModelError("EditInput.Monto", "El monto debe ser mayor que cero.");

        if (!ModelState.IsValid)
        {
            await CargarAsync(user.Id);
            return Page();
        }

        var prestamo = await _dbContext.Prestamos
            .FirstOrDefaultAsync(p => p.Id == EditInput.Id && p.UsuarioId == user.Id);

        if (prestamo is not null)
        {
            prestamo.Monto = EditInput.Monto;
            prestamo.Persona = EditInput.Persona.Trim();
            prestamo.FechaPrestamo = EditInput.FechaPrestamo;
            prestamo.FechaEstimadaDevolucion = EditInput.FechaEstimadaDevolucion;
            prestamo.Notas = string.IsNullOrWhiteSpace(EditInput.Notas) ? null : EditInput.Notas.Trim();
            await _dbContext.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task CargarAsync(string userId)
    {
        Prestamos = await _dbContext.Prestamos
            .AsNoTracking()
            .Where(p => p.UsuarioId == userId)
            .OrderByDescending(p => p.FechaPrestamo)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

        PrestamosPendientes = Prestamos.Where(p => p.Estado == EstadoPrestamo.Pendiente).ToList();
        TotalPendiente = PrestamosPendientes.Sum(p => p.Monto);
    }

    // ── View models ───────────────────────────────────────────────────────────
    public class InputModel
    {
        [Required(ErrorMessage = "El monto es obligatorio.")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "La persona es obligatoria.")]
        [StringLength(120, ErrorMessage = "Máximo 120 caracteres.")]
        public string Persona { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime FechaPrestamo { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime? FechaEstimadaDevolucion { get; set; }

        [StringLength(200)]
        public string? Notas { get; set; }
    }

    public class EditInputModel
    {
        public int Id { get; set; }

        public decimal Monto { get; set; }

        [StringLength(120, ErrorMessage = "Máximo 120 caracteres.")]
        public string Persona { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime FechaPrestamo { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime? FechaEstimadaDevolucion { get; set; }

        [StringLength(200)]
        public string? Notas { get; set; }
    }
}
