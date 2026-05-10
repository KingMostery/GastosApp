namespace GastosApp.Data;

public static class GastoDisplayExtensions
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

    public static string ToDisplayName(this FuenteIngreso value) => value switch
    {
        FuenteIngreso.Salario => "Salario",
        FuenteIngreso.Freelance => "Freelance",
        FuenteIngreso.Negocio => "Negocio",
        FuenteIngreso.Inversion => "Inversión",
        FuenteIngreso.Transferencia => "Transferencia",
        _ => "Otro"
    };

    public static string ToDisplayName(this EstadoPrestamo value) => value switch
    {
        EstadoPrestamo.Pendiente => "Pendiente",
        EstadoPrestamo.Devuelto => "Devuelto",
        _ => "Desconocido"
    };
}
