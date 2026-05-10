using System.ComponentModel.DataAnnotations;

namespace GastosApp.Data;

public class Prestamo
{
    public int Id { get; set; }

    public decimal Monto { get; set; }

    [Required]
    [StringLength(120)]
    public string Persona { get; set; } = string.Empty;

    public DateTime FechaPrestamo { get; set; }

    public DateTime? FechaEstimadaDevolucion { get; set; }

    public EstadoPrestamo Estado { get; set; } = EstadoPrestamo.Pendiente;

    [StringLength(200)]
    public string? Notas { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    public ApplicationUser? Usuario { get; set; }
}

public enum EstadoPrestamo
{
    Pendiente = 1,
    Devuelto = 2
}
