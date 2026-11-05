using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HrApi.Tests.Support;

namespace HrApi.Tests;

/// <summary>Mantenimiento de países, departamentos y municipios (RF-01, RF-02, RF-03, RF-07, RF-08).</summary>
public class GeografiaTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _c = factory.CreateClient();

    // ---------------------------------------------------------------- Países
    [Fact, Trait("Caso", "TC-PAIS-01"), Trait("Req", "RF-01")]
    public async Task Crear_pais_valido_devuelve_201_con_Location_y_estructura()
    {
        var r = await _c.PostAsJsonAsync("/api/paises", new { nombre = Datos.Unico("Pais") });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.NotNull(r.Headers.Location);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(j.GetProperty("id").GetInt32() > 0);
        Assert.False(string.IsNullOrWhiteSpace(j.GetProperty("nombre").GetString()));
    }

    [Theory, Trait("Caso", "TC-PAIS-02"), Trait("Req", "RF-01")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Crear_pais_sin_nombre_devuelve_400(string? nombre)
    {
        var r = await _c.PostAsJsonAsync("/api/paises", new { nombre });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-PAIS-03"), Trait("Req", "RF-01")]
    public async Task Crear_pais_duplicado_ignorando_mayusculas_devuelve_409()
    {
        var nombre = Datos.Unico("Pais");
        await Datos.Crear(_c, "/api/paises", new { nombre });

        var r = await _c.PostAsJsonAsync("/api/paises", new { nombre = nombre.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-PAIS-04"), Trait("Req", "RF-08")]
    public async Task Obtener_pais_inexistente_devuelve_404()
    {
        var r = await _c.GetAsync("/api/paises/999999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-PAIS-05"), Trait("Req", "RF-01")]
    public async Task Actualizar_pais_cambia_el_nombre()
    {
        var p = await Datos.Crear(_c, "/api/paises", new { nombre = Datos.Unico("Pais") });
        var id = p.GetProperty("id").GetInt32();
        var nuevo = Datos.Unico("Nuevo");

        var r = await _c.PutAsJsonAsync($"/api/paises/{id}", new { nombre = nuevo });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(nuevo, j.GetProperty("nombre").GetString());
    }

    [Fact, Trait("Caso", "TC-PAIS-06"), Trait("Req", "RF-01")]
    public async Task Eliminar_pais_sin_dependientes_devuelve_204()
    {
        var p = await Datos.Crear(_c, "/api/paises", new { nombre = Datos.Unico("Pais") });
        var r = await _c.DeleteAsync($"/api/paises/{p.GetProperty("id").GetInt32()}");
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-PAIS-07"), Trait("Req", "RF-07")]
    [Trait("Defecto", "DEF-004"), Trait("Category", "KnownDefect")]
    public async Task Eliminar_pais_con_departamentos_devuelve_409()
    {
        var g = await Datos.NuevaGeo(_c);

        var r = await _c.DeleteAsync($"/api/paises/{g.PaisId}");

        // DEF-004: la API responde 500 por la violación de FK.
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    // --------------------------------------------------------- Departamentos
    [Fact, Trait("Caso", "TC-DEP-01"), Trait("Req", "RF-02")]
    public async Task Crear_departamento_valido_devuelve_201()
    {
        var pais = await Datos.Crear(_c, "/api/paises", new { nombre = Datos.Unico("Pais") });
        var paisId = pais.GetProperty("id").GetInt32();

        var r = await _c.PostAsJsonAsync("/api/departamentos", new { nombre = Datos.Unico("Dep"), paisId });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(paisId, j.GetProperty("paisId").GetInt32());
    }

    [Fact, Trait("Caso", "TC-DEP-02"), Trait("Req", "RF-02")]
    public async Task Crear_departamento_con_pais_inexistente_devuelve_400()
    {
        var r = await _c.PostAsJsonAsync("/api/departamentos", new { nombre = Datos.Unico("Dep"), paisId = 999999 });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-DEP-03"), Trait("Req", "RF-02")]
    public async Task Crear_departamento_duplicado_en_el_mismo_pais_devuelve_409()
    {
        var pais = await Datos.Crear(_c, "/api/paises", new { nombre = Datos.Unico("Pais") });
        var paisId = pais.GetProperty("id").GetInt32();
        var nombre = Datos.Unico("Dep");
        await Datos.Crear(_c, "/api/departamentos", new { nombre, paisId });

        var r = await _c.PostAsJsonAsync("/api/departamentos", new { nombre, paisId });

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-DEP-04"), Trait("Req", "RF-02")]
    public async Task Mismo_nombre_de_departamento_en_paises_distintos_es_valido()
    {
        var nombre = Datos.Unico("Dep");
        var p1 = (await Datos.Crear(_c, "/api/paises", new { nombre = Datos.Unico("P1") })).GetProperty("id").GetInt32();
        var p2 = (await Datos.Crear(_c, "/api/paises", new { nombre = Datos.Unico("P2") })).GetProperty("id").GetInt32();
        await Datos.Crear(_c, "/api/departamentos", new { nombre, paisId = p1 });

        var r = await _c.PostAsJsonAsync("/api/departamentos", new { nombre, paisId = p2 });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-DEP-05"), Trait("Req", "RF-07")]
    public async Task Eliminar_departamento_con_municipios_devuelve_409()
    {
        var g = await Datos.NuevaGeo(_c);
        var r = await _c.DeleteAsync($"/api/departamentos/{g.DepartamentoId}");
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    // ------------------------------------------------------------ Municipios
    [Fact, Trait("Caso", "TC-MUN-01"), Trait("Req", "RF-03")]
    public async Task Crear_municipio_valido_devuelve_201()
    {
        var g = await Datos.NuevaGeo(_c);
        var r = await _c.PostAsJsonAsync("/api/municipios",
            new { nombre = Datos.Unico("Mun"), departamentoId = g.DepartamentoId });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-MUN-02"), Trait("Req", "RF-03")]
    public async Task Crear_municipio_con_departamento_inexistente_devuelve_400()
    {
        var r = await _c.PostAsJsonAsync("/api/municipios",
            new { nombre = Datos.Unico("Mun"), departamentoId = 999999 });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-MUN-03"), Trait("Req", "RF-03")]
    public async Task Crear_municipio_duplicado_en_el_mismo_departamento_devuelve_409()
    {
        var g = await Datos.NuevaGeo(_c);
        var nombre = Datos.Unico("Mun");
        await Datos.Crear(_c, "/api/municipios", new { nombre, departamentoId = g.DepartamentoId });

        var r = await _c.PostAsJsonAsync("/api/municipios", new { nombre, departamentoId = g.DepartamentoId });

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-MUN-04"), Trait("Req", "RF-03")]
    public async Task Listar_municipios_filtra_por_departamento()
    {
        var g = await Datos.NuevaGeo(_c);

        var lista = await _c.GetFromJsonAsync<JsonElement>($"/api/municipios?departamentoId={g.DepartamentoId}");

        Assert.Equal(1, lista.GetArrayLength());
        Assert.Equal(g.MunicipioId, lista[0].GetProperty("id").GetInt32());
    }
}
