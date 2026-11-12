using Microsoft.EntityFrameworkCore;
using OfertaAcademica.Web.Data;
using OfertaAcademica.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.Cookie.Name = "OfertaAcademica.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<ICorreoInstitucionalService, CorreoInstitucionalService>();

var databaseDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(databaseDirectory);
var databasePath = Path.Combine(databaseDirectory, "oferta_academica.db");
var legacyDatabasePath = Path.Combine(builder.Environment.ContentRootPath, "oferta_academica.db");
if (!File.Exists(databasePath) && File.Exists(legacyDatabasePath))
    File.Copy(legacyDatabasePath, databasePath);
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite($"Data Source={databasePath}"));

builder.Services.AddScoped<IAsignaturaService, AsignaturaServiceEF>();
builder.Services.AddScoped<IProfesorService,   ProfesorServiceEF>();
builder.Services.AddScoped<ISalonService,      SalonServiceEF>();
builder.Services.AddScoped<GeneradorOfertaService>();
builder.Services.AddScoped<ExcelOfertaService>();

var app = builder.Build();

// Aplica las migraciones pendientes y carga datos iniciales al arrancar.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    SeedData.Initialize(db);
    PregradoSeed.Initialize(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
