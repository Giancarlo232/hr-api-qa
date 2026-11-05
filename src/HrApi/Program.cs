using HrApi.Data;
using HrApi.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// La cadena se lee de forma diferida para que las pruebas puedan reemplazarla (UseSetting).
builder.Services.AddDbContext<AppDbContext>((sp, o) =>
    o.UseSqlite(sp.GetRequiredService<IConfiguration>().GetConnectionString("Default") ?? "Data Source=hr.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGeografia();
app.MapEmpresas();
app.MapColaboradores();

app.Run();

// Necesario para WebApplicationFactory<Program> en el proyecto de pruebas.
public partial class Program { }
