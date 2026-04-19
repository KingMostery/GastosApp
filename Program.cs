using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GastosApp.Data;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Base de datos (SQLite GRATIS)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=gastos.db"));

// 🔹 Identity (login SIN roles)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    // Config simple para pruebas
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// 🔹 Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// 🔹 Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// 🔹 IMPORTANTE (login)
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();