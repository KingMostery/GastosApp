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
        }
    }
}