using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GastosApp.Data;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Base de datos (SQLite GRATIS)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=gastos.db"));

// 🔹 Identity (login SIN roles)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
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
app.UseStaticFiles();
app.UseRouting();

// 🔹 IMPORTANTE (login)
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();