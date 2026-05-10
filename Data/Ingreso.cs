using System.ComponentModel.DataAnnotations;

namespace GastosApp.Data;

public class Ingreso
{
    public int Id { get; set; }

    public decimal Valor { get; set; }

    public DateTime Fecha { get; set; }

    [Required]
    [StringLength(120)]
    public string Concepto { get; set; } = string.Empty;

    public FuenteIngreso Fuente { get; set; }

    [StringLength(200)]
    public string? Descripcion { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    public ApplicationUser? Usuario { get; set; }
}

public enum FuenteIngreso
{
    Salario = 1,
    Freelance = 2,
    Negocio = 3,
    Inversion = 4,
    Transferencia = 5,
    Otro = 6
}
