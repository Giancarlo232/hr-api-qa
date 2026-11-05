using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HrApi.Tests.Support;

namespace HrApi.Tests;

/// <summary>Mantenimiento de colaboradores y su pertenencia a empresas (RF-05, RF-06, RF-08).</summary>
public class ColaboradoresTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _c = factory.CreateClient();

    [Fact, Trait("Caso", "TC-COL-01"), Trait("Req", "RF-05")]
    public async Task Crear_colaborador_en_una_empresa_devuelve_201_y_estructura()
    {
        var empresaId = await Datos.NuevaEmpresa(_c);

        var r = await _c.PostAsJsonAsync("/api/colaboradores", Datos.ColaboradorBody([empresaId]));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        string[] esperadas = ["id", "nombreCompleto", "edad", "telefono", "email", "empresaIds"];
        var presentes = j.EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.True(esperadas.All(presentes.Contains));
        Assert.Equal(empresaId, j.GetProperty("empresaIds")[0].GetInt32());
    }

    [Fact, Trait("Caso", "TC-COL-02"), Trait("Req", "RF-06")]
    public async Task Crear_colaborador_asociado_a_varias_empresas()
    {
        var e1 = await Datos.NuevaEmpresa(_c);
        var e2 = await Datos.NuevaEmpresa(_c);

        var j = await Datos.Crear(_c, "/api/colaboradores", Datos.ColaboradorBody([e1, e2]));

        var ids = j.GetProperty("empresaIds").EnumerateArray().Select(x => x.GetInt32()).ToList();
        Assert.Equal(2, ids.Count);
        Assert.Contains(e1, ids);
        Assert.Contains(e2, ids);
    }

    [Fact, Trait("Caso", "TC-COL-03"), Trait("Req", "RF-05")]
    public async Task Crear_colaborador_sin_empresa_devuelve_400()
    {
        var r = await _c.PostAsJsonAsync("/api/colaboradores", Datos.ColaboradorBody([]));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-COL-04"), Trait("Req", "RF-05")]
    public async Task Crear_colaborador_con_empresa_inexistente_devuelve_400()
    {
        var r = await _c.PostAsJsonAsync("/api/colaboradores", Datos.ColaboradorBody([999999]));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Theory, Trait("Caso", "TC-COL-05"), Trait("Req", "RF-05")]
    [InlineData("nombreCompleto")]
    [InlineData("telefono")]
    [InlineData("email")]
    public async Task Crear_colaborador_sin_campo_obligatorio_devuelve_400(string campo)
    {
        var empresaId = await Datos.NuevaEmpresa(_c);
        var body = new Dictionary<string, object?>
        {
            ["nombreCompleto"] = "Ana Pérez", ["edad"] = 30, ["telefono"] = "55667788",
            ["email"] = "a@a.com", ["empresaIds"] = new[] { empresaId }
        };
        body[campo] = "";

        var r = await _c.PostAsJsonAsync("/api/colaboradores", body);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-COL-06"), Trait("Req", "RF-05")]
    public async Task Crear_colaborador_con_edad_mayor_al_maximo_devuelve_400()
    {
        var empresaId = await Datos.NuevaEmpresa(_c);
        var r = await _c.PostAsJsonAsync("/api/colaboradores", Datos.ColaboradorBody([empresaId], edad: 121));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Theory, Trait("Caso", "TC-COL-07"), Trait("Req", "RF-05")]
    [Trait("Defecto", "DEF-002"), Trait("Category", "KnownDefect")]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Crear_colaborador_con_edad_no_positiva_devuelve_400(int edad)
    {
        var empresaId = await Datos.NuevaEmpresa(_c);

        var r = await _c.PostAsJsonAsync("/api/colaboradores", Datos.ColaboradorBody([empresaId], edad));

        // DEF-002: la API acepta 0 y negativos (201).
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Theory, Trait("Caso", "TC-COL-08"), Trait("Req", "RF-05")]
    [Trait("Defecto", "DEF-005"), Trait("Category", "KnownDefect")]
    [InlineData("sin-arroba")]
    [InlineData("a@b")]
    public async Task Crear_colaborador_con_correo_invalido_devuelve_400(string email)
    {
        var empresaId = await Datos.NuevaEmpresa(_c);

        var r = await _c.PostAsJsonAsync("/api/colaboradores", Datos.ColaboradorBody([empresaId], email: email));

        // DEF-005: la API acepta cualquier texto como correo (201).
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-COL-09"), Trait("Req", "RF-08")]
    [Trait("Defecto", "DEF-006"), Trait("Category", "KnownDefect")]
    public async Task Obtener_colaborador_inexistente_devuelve_404()
    {
        var r = await _c.GetAsync("/api/colaboradores/999999");

        // DEF-006: la API responde 200 con cuerpo nulo.
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-COL-10"), Trait("Req", "RF-06")]
    public async Task Asociar_colaborador_a_una_empresa_adicional()
    {
        var e1 = await Datos.NuevaEmpresa(_c);
        var e2 = await Datos.NuevaEmpresa(_c);
        var col = await Datos.Crear(_c, "/api/colaboradores", Datos.ColaboradorBody([e1]));
        var id = col.GetProperty("id").GetInt32();

        var r = await _c.PostAsync($"/api/colaboradores/{id}/empresas/{e2}", null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, j.GetProperty("empresaIds").GetArrayLength());
    }

    [Fact, Trait("Caso", "TC-COL-11"), Trait("Req", "RF-06")]
    [Trait("Defecto", "DEF-007"), Trait("Category", "KnownDefect")]
    public async Task Asociar_colaborador_dos_veces_a_la_misma_empresa_devuelve_409()
    {
        var e1 = await Datos.NuevaEmpresa(_c);
        var col = await Datos.Crear(_c, "/api/colaboradores", Datos.ColaboradorBody([e1]));
        var id = col.GetProperty("id").GetInt32();

        var r = await _c.PostAsync($"/api/colaboradores/{id}/empresas/{e1}", null);

        // DEF-007: la API duplica la relación y responde 200.
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact, Trait("Caso", "TC-COL-12"), Trait("Req", "RF-06")]
    public async Task Crear_colaborador_con_empresas_repetidas_en_el_cuerpo_las_deduplica()
    {
        var e1 = await Datos.NuevaEmpresa(_c);

        var j = await Datos.Crear(_c, "/api/colaboradores", Datos.ColaboradorBody([e1, e1]));

        Assert.Equal(1, j.GetProperty("empresaIds").GetArrayLength());
    }

    [Fact, Trait("Caso", "TC-COL-13"), Trait("Req", "RF-05")]
    public async Task Actualizar_colaborador_reemplaza_sus_empresas()
    {
        var e1 = await Datos.NuevaEmpresa(_c);
        var e2 = await Datos.NuevaEmpresa(_c);
        var col = await Datos.Crear(_c, "/api/colaboradores", Datos.ColaboradorBody([e1]));
        var id = col.GetProperty("id").GetInt32();

        var r = await _c.PutAsJsonAsync($"/api/colaboradores/{id}", Datos.ColaboradorBody([e2], edad: 41));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(41, j.GetProperty("edad").GetInt32());
        var ids = j.GetProperty("empresaIds").EnumerateArray().Select(x => x.GetInt32()).ToList();
        Assert.Equal([e2], ids);
    }

    [Fact, Trait("Caso", "TC-COL-14"), Trait("Req", "RF-08")]
    public async Task Actualizar_o_eliminar_colaborador_inexistente_devuelve_404()
    {
        var e1 = await Datos.NuevaEmpresa(_c);

        var put = await _c.PutAsJsonAsync("/api/colaboradores/999999", Datos.ColaboradorBody([e1]));
        var del = await _c.DeleteAsync("/api/colaboradores/999999");

        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, del.StatusCode);
    }

    [Fact, Trait("Caso", "TC-COL-15"), Trait("Req", "RF-05")]
    public async Task Eliminar_colaborador_elimina_sus_asociaciones_y_permite_borrar_la_empresa()
    {
        var e1 = await Datos.NuevaEmpresa(_c);
        var col = await Datos.Crear(_c, "/api/colaboradores", Datos.ColaboradorBody([e1]));
        var id = col.GetProperty("id").GetInt32();

        var delCol = await _c.DeleteAsync($"/api/colaboradores/{id}");
        var delEmp = await _c.DeleteAsync($"/api/empresas/{e1}");

        Assert.Equal(HttpStatusCode.NoContent, delCol.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delEmp.StatusCode);
    }

    [Fact, Trait("Caso", "TC-COL-16"), Trait("Req", "RF-09")]
    public async Task Buscar_colaboradores_por_nombre_filtra_resultados()
    {
        var e1 = await Datos.NuevaEmpresa(_c);
        var marca = Datos.Unico("Zzq");
        await Datos.Crear(_c, "/api/colaboradores", new
        {
            nombreCompleto = $"{marca} Gómez", edad = 28, telefono = "1", email = "z@z.com", empresaIds = new[] { e1 }
        });

        var lista = await _c.GetFromJsonAsync<JsonElement>($"/api/colaboradores?nombre={marca}");

        Assert.Equal(1, lista.GetArrayLength());
    }
}
