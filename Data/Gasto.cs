using System.ComponentModel.DataAnnotations;

namespace GastosApp.Data;

public class Gasto
{
    public int Id { get; set; }

    public decimal Valor { get; set; }

    public DateTime Fecha { get; set; }

    public MetodoPago MetodoPago { get; set; }

    public CategoriaGasto Categoria { get; set; }

    [StringLength(160)]
    public string? Descripcion { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    public ApplicationUser? Usuario { get; set; }
}

public enum MetodoPago
{
    Efectivo = 1,
    TarjetaDebito = 2,
    TarjetaCredito = 3,
    Transferencia = 4,
    Nequi = 5,
    Daviplata = 6,
    Otro = 7
}

public enum CategoriaGasto
{
    Alimentacion = 1,
    Transporte = 2,
    Hogar = 3,
    Salud = 4,
    Educacion = 5,
    Entretenimiento = 6,
    Servicios = 7,
    Suscripciones = 8,
    Compras = 9,
    Otro = 10
}
