using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HrApi.Tests.Support;

/// <summary>Levanta la API en memoria con una base SQLite temporal propia por clase de pruebas.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"hrapi-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(_dbPath); } catch { /* el archivo puede seguir bloqueado por el pool; no importa */ }
    }
}

public record Geo(int PaisId, int DepartamentoId, int MunicipioId);

/// <summary>Atajos para preparar datos (precondiciones) de cada caso de prueba.</summary>
public static class Datos
{
    private static readonly Random Rng = new();

    public static string Unico(string prefijo) => $"{prefijo}-{Guid.NewGuid():N}"[..20];
    public static string NitUnico() => Rng.NextInt64(10_000_000, 99_999_999).ToString() + "-" + Rng.Next(0, 9);

    public static async Task<JsonElement> Crear(HttpClient c, string url, object body)
    {
        var r = await c.PostAsJsonAsync(url, body);
        r.EnsureSuccessStatusCode(); // 200 o 201: la precondición solo necesita que exista
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    public static async Task<Geo> NuevaGeo(HttpClient c)
    {
        var pais = await Crear(c, "/api/paises", new { nombre = Unico("Pais") });
        var paisId = pais.GetProperty("id").GetInt32();
        var dep = await Crear(c, "/api/departamentos", new { nombre = Unico("Dep"), paisId });
        var depId = dep.GetProperty("id").GetInt32();
        var mun = await Crear(c, "/api/municipios", new { nombre = Unico("Mun"), departamentoId = depId });
        return new Geo(paisId, depId, mun.GetProperty("id").GetInt32());
    }

    public static object EmpresaBody(Geo g, string? nit = null, string? email = "contacto@empresa.com") => new
    {
        nit = nit ?? NitUnico(),
        razonSocial = "Empresa Demo, S.A.",
        nombreComercial = "Demo",
        telefono = "22334455",
        email,
        paisId = g.PaisId,
        departamentoId = g.DepartamentoId,
        municipioId = g.MunicipioId
    };

    public static async Task<int> NuevaEmpresa(HttpClient c, Geo? geo = null)
    {
        geo ??= await NuevaGeo(c);
        var e = await Crear(c, "/api/empresas", EmpresaBody(geo));
        return e.GetProperty("id").GetInt32();
    }

    public static object ColaboradorBody(IEnumerable<int> empresaIds, int edad = 30, string? email = "persona@correo.com") => new
    {
        nombreCompleto = "Ana Pérez",
        edad,
        telefono = "55667788",
        email,
        empresaIds = empresaIds.ToArray()
    };
}
