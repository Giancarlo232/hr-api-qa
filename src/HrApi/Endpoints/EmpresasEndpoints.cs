using HrApi.Data;
using HrApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HrApi.Endpoints;

public static class EmpresasEndpoints
{
    public static void MapEmpresas(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/empresas");

        g.MapGet("", async (int? page, int? size, AppDbContext db) =>
        {
            var p = Math.Max(page ?? 1, 1);
            var s = Math.Clamp(size ?? 50, 1, 200);
            var items = await db.Empresas.AsNoTracking().OrderBy(x => x.Id)
                .Skip((p - 1) * s).Take(s).ToListAsync();
            return Results.Ok(new { page = p, size = s, total = await db.Empresas.CountAsync(), items });
        });

        g.MapGet("{id:int}", async (int id, AppDbContext db) =>
            await db.Empresas.FindAsync(id) is { } e ? Results.Ok(e) : Results.NotFound());

        g.MapPost("", async (EmpresaDto dto, AppDbContext db) =>
        {
            var error = await Validar(dto, db);
            if (error is not null) return Results.BadRequest(new { error });

            // DEF-001 (sembrado): no se verifica NIT duplicado.
            var e = new Empresa();
            Aplicar(e, dto);
            db.Empresas.Add(e);
            await db.SaveChangesAsync();

            // DEF-010 (sembrado): devuelve 200 OK sin Location; lo correcto es 201 Created.
            return Results.Ok(e);
        });

        g.MapPut("{id:int}", async (int id, EmpresaDto dto, AppDbContext db) =>
        {
            var error = await Validar(dto, db);
            if (error is not null) return Results.BadRequest(new { error });

            var e = await db.Empresas.FindAsync(id);
            if (e is null)
            {
                // DEF-009 (sembrado): upsert implícito; debería responder 404.
                e = new Empresa();
                db.Empresas.Add(e);
            }
            Aplicar(e, dto);
            await db.SaveChangesAsync();
            return Results.Ok(e);
        });

        g.MapDelete("{id:int}", async (int id, AppDbContext db) =>
        {
            var e = await db.Empresas.FindAsync(id);
            if (e is null) return Results.NotFound();
            if (await db.ColaboradorEmpresas.AnyAsync(x => x.EmpresaId == id))
                return Results.Conflict(new { error = "Tiene colaboradores asociados" });
            db.Empresas.Remove(e);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static async Task<string?> Validar(EmpresaDto d, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(d.Nit)) return "NIT requerido";
        if (string.IsNullOrWhiteSpace(d.RazonSocial)) return "Razón social requerida";
        if (string.IsNullOrWhiteSpace(d.NombreComercial)) return "Nombre comercial requerido";
        if (string.IsNullOrWhiteSpace(d.Telefono)) return "Teléfono requerido";
        if (!Validators.Email(d.Email)) return "Correo electrónico inválido";
        if (!await db.Paises.AnyAsync(x => x.Id == d.PaisId)) return "País inexistente";
        if (!await db.Departamentos.AnyAsync(x => x.Id == d.DepartamentoId)) return "Departamento inexistente";
        if (!await db.Municipios.AnyAsync(x => x.Id == d.MunicipioId)) return "Municipio inexistente";
        // DEF-003 (sembrado): no se valida que el departamento pertenezca al país
        // ni que el municipio pertenezca al departamento.
        return null;
    }

    private static void Aplicar(Empresa e, EmpresaDto d)
    {
        e.Nit = d.Nit!.Trim();
        e.RazonSocial = d.RazonSocial!.Trim();
        e.NombreComercial = d.NombreComercial!.Trim();
        e.Telefono = d.Telefono!.Trim();
        e.Email = d.Email!.Trim();
        e.PaisId = d.PaisId;
        e.DepartamentoId = d.DepartamentoId;
        e.MunicipioId = d.MunicipioId;
    }
}
