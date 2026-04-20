using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using GastosApp.Data;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Base de datos (SQLite GRATIS)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=gastos.db"));

// 🔹 Identity (login SIN roles)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    // Config simple para pruebas
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

// 🔹 Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// Crea las tablas de Identity automaticamente si aun no existen.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();

    // Agrega la columna si la base ya existia antes de ApplicationUser.
    var connection = dbContext.Database.GetDbConnection();
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = "PRAGMA table_info('AspNetUsers');";
    using var reader = command.ExecuteReader();

    var hasNombreCompleto = false;
    while (reader.Read())
    {
        if (string.Equals(reader.GetString(1), "NombreCompleto", StringComparison.OrdinalIgnoreCase))
        {
            hasNombreCompleto = true;
            break;
        }
    }

    if (!hasNombreCompleto)
    {
        dbContext.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN NombreCompleto TEXT NOT NULL DEFAULT ''");
    }

    dbContext.Database.ExecuteSqlRaw(
        @"CREATE TABLE IF NOT EXISTS Gastos (
            Id INTEGER NOT NULL CONSTRAINT PK_Gastos PRIMARY KEY AUTOINCREMENT,
            Valor TEXT NOT NULL,
            Fecha TEXT NOT NULL,
            MetodoPago TEXT NOT NULL,
            Categoria TEXT NOT NULL,
            UsuarioId TEXT NOT NULL,
            Descripcion TEXT NULL,
            CONSTRAINT FK_Gastos_AspNetUsers_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
        );");

    dbContext.Database.ExecuteSqlRaw(
        @"CREATE INDEX IF NOT EXISTS IX_Gastos_UsuarioId ON Gastos (UsuarioId);");
}

// 🔹 Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 🔹 IMPORTANTE (login)
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();