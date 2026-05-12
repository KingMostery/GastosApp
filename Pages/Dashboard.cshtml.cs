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

    // ── Gastos generales ──────────────────────────────────────────────────────
    public decimal TotalUltimos30Dias { get; private set; }
    public decimal TotalUltimos15Dias { get; private set; }
    public int MovimientosUltimos30Dias { get; private set; }
    public int MovimientosUltimos15Dias { get; private set; }
    public List<Gasto> GastosRecientes { get; private set; } = [];
    public List<CategoriaResumen> ResumenPorCategoria { get; private set; } = [];

    // ── Balance del mes ───────────────────────────────────────────────────────
    public decimal IngresosMes { get; private set; }
    public decimal GastosMes { get; private set; }
    public decimal BalanceMes => IngresosMes - GastosMes;

    // Tasa de ahorro: Ahorro / Ingresos * 100 (0 si no hay ingresos)
    public decimal TasaAhorro => IngresosMes > 0
        ? Math.Round(BalanceMes / IngresosMes * 100, 1)
        : 0;

    // Porcentaje del ingreso ya gastado (para barra de progreso)
    public decimal PorcentajeGastado => IngresosMes > 0
        ? Math.Min(Math.Round(GastosMes / IngresosMes * 100, 1), 100)
        : (GastosMes > 0 ? 100 : 0);

    // ── Categoría top ─────────────────────────────────────────────────────────
    public CategoriaResumen? CategoriaTop { get; private set; }

    // ── Promedio diario y proyección ──────────────────────────────────────────
    public decimal PromedioDiario { get; private set; }
    public decimal ProyeccionMes { get; private set; }
    public int DiasTranscurridos { get; private set; }

    // ── Comparativo mes anterior ──────────────────────────────────────────────
    public decimal IngresosMesAnterior { get; private set; }
    public decimal GastosMesAnterior { get; private set; }
    public decimal BalanceMesAnterior => IngresosMesAnterior - GastosMesAnterior;

    // ── Ingresos recientes ────────────────────────────────────────────────────
    public List<Ingreso> IngresosRecientes { get; private set; } = [];

    // ── Préstamos ─────────────────────────────────────────────────────────────
    public int PrestamosPendientes { get; private set; }
    public decimal TotalPrestadoPendiente { get; private set; }
    public List<Prestamo> PrestamosVencidos { get; private set; } = [];
    public List<Prestamo> PrestamosPendientesList { get; private set; } = [];

    // ── Gamificación ──────────────────────────────────────────────────────────
    public int PuntajeFinanciero { get; private set; }
    public int RachaAhorro { get; private set; }    public bool SinDatosGamificacion { get; private set; }    public List<Logro> Logros { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        UsuarioActual = user;
        await CargarDashboardAsync(user.Id);
        return Page();
    }

    private async Task CargarDashboardAsync(string userId)
    {
        var hoy = DateTime.Today;
        var desde30 = hoy.AddDays(-30);
        var desde15 = hoy.AddDays(-15);
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var inicioMesAnterior = inicioMes.AddMonths(-1);
        var finMesAnterior = inicioMes.AddDays(-1);

        DiasTranscurridos = Math.Max(hoy.Day, 1);
        var diasTotalesMes = DateTime.DaysInMonth(hoy.Year, hoy.Month);

        // ── Gastos ────────────────────────────────────────────────────────────
        var todosGastos = await _dbContext.Gastos
            .AsNoTracking()
            .Where(g => g.UsuarioId == userId)
            .OrderByDescending(g => g.Fecha)
            .ThenByDescending(g => g.Id)
            .ToListAsync();

        var gastos30 = todosGastos.Where(g => g.Fecha.Date >= desde30).ToList();
        var gastos15 = todosGastos.Where(g => g.Fecha.Date >= desde15).ToList();
        var gastosMesActual = todosGastos.Where(g => g.Fecha.Date >= inicioMes).ToList();
        var gastosMesAnt = todosGastos.Where(g => g.Fecha.Date >= inicioMesAnterior && g.Fecha.Date <= finMesAnterior).ToList();

        TotalUltimos30Dias = gastos30.Sum(g => g.Valor);
        TotalUltimos15Dias = gastos15.Sum(g => g.Valor);
        MovimientosUltimos30Dias = gastos30.Count;
        MovimientosUltimos15Dias = gastos15.Count;
        GastosRecientes = gastos30.Take(5).ToList();
        GastosMes = gastosMesActual.Sum(g => g.Valor);
        GastosMesAnterior = gastosMesAnt.Sum(g => g.Valor);

        ResumenPorCategoria = gastosMesActual
            .GroupBy(g => g.Categoria)
            .Select(gr => new CategoriaResumen
            {
                Categoria = gr.Key,
                Total = gr.Sum(g => g.Valor),
                Cantidad = gr.Count()
            })
            .OrderByDescending(r => r.Total)
            .Take(5)
            .ToList();

        CategoriaTop = ResumenPorCategoria.FirstOrDefault();

        PromedioDiario = DiasTranscurridos > 0 ? GastosMes / DiasTranscurridos : 0;
        ProyeccionMes = PromedioDiario * diasTotalesMes;

        // ── Ingresos ──────────────────────────────────────────────────────────
        var todosIngresos = await _dbContext.Ingresos
            .AsNoTracking()
            .Where(i => i.UsuarioId == userId)
            .OrderByDescending(i => i.Fecha)
            .ThenByDescending(i => i.Id)
            .ToListAsync();

        IngresosMes = todosIngresos.Where(i => i.Fecha >= inicioMes).Sum(i => i.Valor);
        IngresosMesAnterior = todosIngresos
            .Where(i => i.Fecha.Date >= inicioMesAnterior && i.Fecha.Date <= finMesAnterior)
            .Sum(i => i.Valor);
        IngresosRecientes = todosIngresos.Take(3).ToList();

        // ── Préstamos ─────────────────────────────────────────────────────────
        var todosPrestamos = await _dbContext.Prestamos
            .AsNoTracking()
            .Where(p => p.UsuarioId == userId && p.Estado == EstadoPrestamo.Pendiente)
            .ToListAsync();

        todosPrestamos = todosPrestamos
            .OrderBy(p => p.FechaEstimadaDevolucion)
            .ThenByDescending(p => p.Monto)
            .ToList();

        PrestamosPendientesList = todosPrestamos;
        PrestamosPendientes = todosPrestamos.Count;
        TotalPrestadoPendiente = todosPrestamos.Sum(p => p.Monto);
        PrestamosVencidos = todosPrestamos
            .Where(p => p.FechaEstimadaDevolucion.HasValue && p.FechaEstimadaDevolucion.Value.Date < hoy)
            .ToList();

        // ── Gamificación ──────────────────────────────────────────────────────
        CalcularGamificacion(gastosMesActual, todosGastos, diasTotalesMes, hoy);
    }

    private void CalcularGamificacion(
        List<Gasto> gastosMesActual,
        List<Gasto> todosGastos,
        int diasTotalesMes,
        DateTime hoy)
    {
        // ── Racha de ahorro ───────────────────────────────────────────────────
        // Sin actividad alguna → racha = 0 (evita contar días vacíos como "en racha")
        bool sinActividad = todosGastos.Count == 0 && IngresosMes == 0 && PrestamosPendientesList.Count == 0;
        SinDatosGamificacion = sinActividad;

        if (!sinActividad && (gastosMesActual.Count > 0 || IngresosMes > 0))
        {
            decimal presupuestoDiario = IngresosMes > 0 && diasTotalesMes > 0
                ? IngresosMes / diasTotalesMes
                : (PromedioDiario > 0 ? PromedioDiario * 1.2m : 0);

            var gastosPorDia = gastosMesActual
                .GroupBy(g => g.Fecha.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Valor));

            int racha = 0;
            for (int d = hoy.Day; d >= 1; d--)
            {
                var fecha = new DateTime(hoy.Year, hoy.Month, d);
                var gastadoDia = gastosPorDia.ContainsKey(fecha) ? gastosPorDia[fecha] : 0;
                if (presupuestoDiario == 0 ? gastadoDia == 0 : gastadoDia <= presupuestoDiario)
                    racha++;
                else
                    break;
            }
            RachaAhorro = racha;
        }
        else
        {
            RachaAhorro = 0;
        }

        // ── Puntuación financiera (0-100) ────────────────────────────────────
        // Usuario sin ningún dato → puntaje 0, no tiene sentido calcular
        if (sinActividad)
        {
            PuntajeFinanciero = 0;
            Logros = BuildLogros(todosGastos);
            return;
        }

        int score = 0;

        // 1. Tasa de ahorro del mes: hasta 35 pts
        score += TasaAhorro switch
        {
            > 30 => 35,
            > 20 => 28,
            > 10 => 20,
            > 0  => 10,
            _    => 0
        };

        // 2. Sin préstamos vencidos: 20 pts (solo si el usuario tiene préstamos activos)
        if (PrestamosPendientesList.Count > 0 && PrestamosVencidos.Count == 0) score += 20;

        // 3. Proyección dentro del ingreso del mes: 20 pts
        if (IngresosMes > 0 && ProyeccionMes <= IngresosMes) score += 20;

        // 4. Tiene ingresos registrados este mes: 10 pts
        if (IngresosMes > 0) score += 10;

        // 5. Racha de ahorro >= 5 días: 5 pts; >= 10 días: 10 pts
        if (RachaAhorro >= 10) score += 10;
        else if (RachaAhorro >= 5) score += 5;

        // 6. Gastos distribuidos (al menos 5 gastos en el mes): 5 pts
        if (gastosMesActual.Count >= 5) score += 5;

        PuntajeFinanciero = Math.Min(score, 100);

        // ── Logros ────────────────────────────────────────────────────────────
        Logros = BuildLogros(todosGastos);
    }

    private List<Logro> BuildLogros(List<Gasto> todosGastos) =>
        [
            new Logro
            {
                Icono = "📝",
                Nombre = "Primer paso",
                Descripcion = "Registraste tu primer gasto",
                Desbloqueado = todosGastos.Count >= 1
            },
            new Logro
            {
                Icono = "💰",
                Nombre = "Ahorrador del mes",
                Descripcion = "Cerraste el mes con balance positivo",
                Desbloqueado = BalanceMes > 0
            },
            new Logro
            {
                Icono = "🌟",
                Nombre = "Gran ahorrador",
                Descripcion = "Tasa de ahorro superior al 20% este mes",
                Desbloqueado = TasaAhorro >= 20
            },
            new Logro
            {
                Icono = "📊",
                Nombre = "Organizado",
                Descripcion = "Tienes más de 10 gastos registrados en total",
                Desbloqueado = todosGastos.Count >= 10
            },
            new Logro
            {
                Icono = "🏆",
                Nombre = "Sin deudas urgentes",
                Descripcion = "No tienes préstamos vencidos",
                Desbloqueado = PrestamosPendientesList.Count > 0 && PrestamosVencidos.Count == 0
            },
            new Logro
            {
                Icono = "🔥",
                Nombre = "En racha",
                Descripcion = "5 días consecutivos dentro del presupuesto",
                Desbloqueado = RachaAhorro >= 5
            },
            new Logro
            {
                Icono = "📈",
                Nombre = "Ingresos crecientes",
                Descripcion = "Ingresaste más este mes que el mes anterior",
                Desbloqueado = IngresosMes > IngresosMesAnterior && IngresosMes > 0
            },
            new Logro
            {
                Icono = "💎",
                Nombre = "Maestro financiero",
                Descripcion = "Puntuación financiera de 80 o más",
                Desbloqueado = PuntajeFinanciero >= 80
            },
        ];

    public class CategoriaResumen
    {
        public CategoriaGasto Categoria { get; set; }
        public decimal Total { get; set; }
        public int Cantidad { get; set; }
    }
}
