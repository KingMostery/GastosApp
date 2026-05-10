using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GastosApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Gasto> Gastos => Set<Gasto>();
        public DbSet<Ingreso> Ingresos => Set<Ingreso>();
        public DbSet<Prestamo> Prestamos => Set<Prestamo>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Gasto>(entity =>
            {
                entity.Property(gasto => gasto.Valor)
                    .HasPrecision(18, 2);

                entity.Property(gasto => gasto.Categoria)
                    .HasConversion<string>();

                entity.Property(gasto => gasto.MetodoPago)
                    .HasConversion<string>();

                entity.HasOne(gasto => gasto.Usuario)
                    .WithMany()
                    .HasForeignKey(gasto => gasto.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Ingreso>(entity =>
            {
                entity.Property(i => i.Valor).HasPrecision(18, 2);
                entity.Property(i => i.Fuente).HasConversion<string>();
                entity.HasOne(i => i.Usuario)
                    .WithMany()
                    .HasForeignKey(i => i.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Prestamo>(entity =>
            {
                entity.Property(p => p.Monto).HasPrecision(18, 2);
                entity.Property(p => p.Estado).HasConversion<string>();
                entity.HasOne(p => p.Usuario)
                    .WithMany()
                    .HasForeignKey(p => p.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}