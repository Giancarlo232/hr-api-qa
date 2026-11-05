using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HrApi.Tests.Support;

namespace HrApi.Tests;

/// <summary>Mantenimiento de empresas (RF-04, RF-07, RF-08, RF-09).</summary>
public class EmpresasTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _c = factory.CreateClient();

    [Fact, Trait("Caso", "TC-EMP-01"), Trait("Req", "RF-04")]
    [Trait("Defecto", "DEF-010"), Trait("Category", "KnownDefect")]
    public async Task Crear_empresa_valida_devuelve_201_con_Location()
    {
        var g = await Datos.NuevaGeo(_c);

        var r = await _c.PostAsJsonAsync("/api/empresas", Datos.EmpresaBody(g));

        // DEF-010: la API devuelve 200 OK y sin cabecera Location.
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.NotNull(r.Headers.Location);
    }

    [Fact, Trait("Caso", "TC-EMP-02"), Trait("Req", "RF-04"), Trait("Req2", "RF-09")]
    public async Task Crear_empresa_devuelve_la_estructura_esperada()
    {
        var g = await Datos.NuevaGeo(_c);

        var j = await Datos.Crear(_c, "/api/empresas", Datos.EmpresaBody(g));

        string[] esperadas = ["id", "nit", "razonSocial", "nombreComercial", "telefono", "email",
                              "paisId", "departamentoId", "municipioId"];
        var presentes = j.EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.True(esperadas.All(presentes.Contains), $"Faltan campos: {string.Join(",", esperadas.Except(presentes))}");
    }

    [Theory, Trait("Caso", "TC-EMP-03"), Trait("Req", "RF-04")]
    [InlineData("nit")]
    [InlineData("razonSocial")]
    [InlineData("nombreComercial")]
    [InlineData("telefono")]
    public async Task Crear_empresa_sin_campo_obligatorio_devuelve_400(string campo)
    {
        var g = await Datos.NuevaGeo(_c);
        var body = new Dictionary<string, object?>
        {
            ["nit"] = Datos.NitUnico(), ["razonSocial"] = "X, S.A.", ["nombreComercial"] = "X",
            ["telefono"] = "22334455", ["email"] = "x@x.com",
            ["paisId"] = g.PaisId, ["departamentoId"] = g.DepartamentoId, ["municipioId"] = g.MunicipioId
        };
        body[campo] = "";

        var r = await _c.PostAsJsonAsync("/api/empresas", body);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Theory, Trait("Caso", "TC-EMP-04"), Trait("Req", "RF-04")]
    [InlineData("sin-arroba")]
    [InlineData("a@b")]
    [InlineData("")]
    public async Task Crear_empresa_con_correo_invalido_devuelve_400(string email)
    {
        var g = await Datos.NuevaGeo(_c);
        var r = await _c.PostAsJsonAsync("/api/empresas", Datos.EmpresaBody(g, email: email));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-EMP-05"), Trait("Req", "RF-04")]
    [Trait("Defecto", "DEF-001"), Trait("Category", "KnownDefect")]
    public async Task Crear_empresa_con_NIT_duplicado_devuelve_409()
    {
        var g = await Datos.NuevaGeo(_c);
        var nit = Datos.NitUnico();
        await Datos.Crear(_c, "/api/empresas", Datos.EmpresaBody(g, nit));

        var r = await _c.PostAsJsonAsync("/api/empresas", Datos.EmpresaBody(g, nit));

        // DEF-001: la API acepta el duplicado (200).
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-EMP-06"), Trait("Req", "RF-04")]
    public async Task Crear_empresa_con_pais_inexistente_devuelve_400()
    {
        var g = await Datos.NuevaGeo(_c);
        var body = Datos.EmpresaBody(g);
        var json = JsonSerializer.SerializeToElement(body);
        var dict = json.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value);
        dict["paisId"] = 999999;

        var r = await _c.PostAsJsonAsync("/api/empresas", dict);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-EMP-07"), Trait("Req", "RF-04")]
    [Trait("Defecto", "DEF-003"), Trait("Category", "KnownDefect")]
    public async Task Crear_empresa_con_municipio_de_otro_departamento_devuelve_400()
    {
        var g1 = await Datos.NuevaGeo(_c);
        var g2 = await Datos.NuevaGeo(_c);
        // País y departamento de g1, pero municipio de g2: geografía incoherente.
        var incoherente = new Geo(g1.PaisId, g1.DepartamentoId, g2.MunicipioId);

        var r = await _c.PostAsJsonAsync("/api/empresas", Datos.EmpresaBody(incoherente));

        // DEF-003: la API la acepta (200).
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-EMP-08"), Trait("Req", "RF-04")]
    [Trait("Defecto", "DEF-003"), Trait("Category", "KnownDefect")]
    public async Task Crear_empresa_con_departamento_de_otro_pais_devuelve_400()
    {
        var g1 = await Datos.NuevaGeo(_c);
        var g2 = await Datos.NuevaGeo(_c);
        var incoherente = new Geo(g1.PaisId, g2.DepartamentoId, g2.MunicipioId);

        var r = await _c.PostAsJsonAsync("/api/empresas", Datos.EmpresaBody(incoherente));

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-EMP-09"), Trait("Req", "RF-08")]
    public async Task Obtener_empresa_inexistente_devuelve_404()
    {
        var r = await _c.GetAsync("/api/empresas/999999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-EMP-10"), Trait("Req", "RF-08")]
    [Trait("Defecto", "DEF-009"), Trait("Category", "KnownDefect")]
    public async Task Actualizar_empresa_inexistente_devuelve_404()
    {
        var g = await Datos.NuevaGeo(_c);

        var r = await _c.PutAsJsonAsync("/api/empresas/999999", Datos.EmpresaBody(g));

        // DEF-009: la API la crea (upsert) y responde 200.
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-EMP-11"), Trait("Req", "RF-04")]
    public async Task Actualizar_empresa_existente_cambia_los_datos()
    {
        var g = await Datos.NuevaGeo(_c);
        var creada = await Datos.Crear(_c, "/api/empresas", Datos.EmpresaBody(g));
        var id = creada.GetProperty("id").GetInt32();

        var r = await _c.PutAsJsonAsync($"/api/empresas/{id}", new
        {
            nit = creada.GetProperty("nit").GetString(),
            razonSocial = "Razón Nueva, S.A.",
            nombreComercial = "Nueva",
            telefono = "11112222",
            email = "nuevo@empresa.com",
            paisId = g.PaisId, departamentoId = g.DepartamentoId, municipioId = g.MunicipioId
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Razón Nueva, S.A.", j.GetProperty("razonSocial").GetString());
        Assert.Equal(id, j.GetProperty("id").GetInt32());
    }

    [Fact, Trait("Caso", "TC-EMP-12"), Trait("Req", "RF-07")]
    public async Task Eliminar_empresa_con_colaboradores_devuelve_409()
    {
        var empresaId = await Datos.NuevaEmpresa(_c);
        await Datos.Crear(_c, "/api/colaboradores", Datos.ColaboradorBody([empresaId]));

        var r = await _c.DeleteAsync($"/api/empresas/{empresaId}");

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-EMP-13"), Trait("Req", "RF-09")]
    public async Task Listar_empresas_devuelve_pagina_con_estructura_y_en_menos_de_500ms()
    {
        await Datos.NuevaEmpresa(_c);

        var sw = Stopwatch.StartNew();
        var r = await _c.GetAsync("/api/empresas?page=1&size=10");
        sw.Stop();

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, j.GetProperty("page").GetInt32());
        Assert.Equal(10, j.GetProperty("size").GetInt32());
        Assert.True(j.GetProperty("total").GetInt32() >= 1);
        Assert.Equal(JsonValueKind.Array, j.GetProperty("items").ValueKind);
        Assert.True(sw.ElapsedMilliseconds < 500, $"Tiempo: {sw.ElapsedMilliseconds} ms");
    }
}
