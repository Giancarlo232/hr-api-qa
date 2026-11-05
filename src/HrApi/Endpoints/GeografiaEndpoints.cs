using HrApi.Data;
using HrApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HrApi.Endpoints;

public static class GeografiaEndpoints
{
    public static void MapGeografia(this IEndpointRouteBuilder app)
    {
        MapPaises(app.MapGroup("/api/paises"));
        MapDepartamentos(app.MapGroup("/api/departamentos"));
        MapMunicipios(app.MapGroup("/api/municipios"));
    }

    // ---------------------------------------------------------------- Países
    private static void MapPaises(RouteGroupBuilder g)
    {
        g.MapGet("", async (AppDbContext db) =>
            Results.Ok(await db.Paises.AsNoTracking().OrderBy(x => x.Id).ToListAsync()));

        g.MapGet("{id:int}", async (int id, AppDbContext db) =>
            await db.Paises.FindAsync(id) is { } p ? Results.Ok(p) : Results.NotFound());

        g.MapPost("", async (PaisDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre)) return Results.BadRequest(new { error = "Nombre requerido" });
            var nombre = dto.Nombre.Trim();
            if (await db.Paises.AnyAsync(x => x.Nombre.ToLower() == nombre.ToLower()))
                return Results.Conflict(new { error = "País duplicado" });

            var p = new Pais { Nombre = nombre };
            db.Paises.Add(p);
            await db.SaveChangesAsync();
            return Results.Created($"/api/paises/{p.Id}", p);
        });

        g.MapPut("{id:int}", async (int id, PaisDto dto, AppDbContext db) =>
        {
            var p = await db.Paises.FindAsync(id);
            if (p is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(dto.Nombre)) return Results.BadRequest(new { error = "Nombre requerido" });
            var nombre = dto.Nombre.Trim();
            if (await db.Paises.AnyAsync(x => x.Id != id && x.Nombre.ToLower() == nombre.ToLower()))
                return Results.Conflict(new { error = "País duplicado" });
            p.Nombre = nombre;
            await db.SaveChangesAsync();
            return Results.Ok(p);
        });

        g.MapDelete("{id:int}", async (int id, AppDbContext db) =>
        {
            var p = await db.Paises.FindAsync(id);
            if (p is null) return Results.NotFound();
            // DEF-004 (sembrado): no se verifica si tiene departamentos; la FK revienta con 500 en lugar de 409.
            db.Paises.Remove(p);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    // --------------------------------------------------------- Departamentos
    private static void MapDepartamentos(RouteGroupBuilder g)
    {
        g.MapGet("", async (int? paisId, AppDbContext db) =>
        {
            var q = db.Departamentos.AsNoTracking().AsQueryable();
            if (paisId is not null) q = q.Where(x => x.PaisId == paisId);
            return Results.Ok(await q.OrderBy(x => x.Id).ToListAsync());
        });

        g.MapGet("{id:int}", async (int id, AppDbContext db) =>
            await db.Departamentos.FindAsync(id) is { } d ? Results.Ok(d) : Results.NotFound());

        g.MapPost("", async (DepartamentoDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre)) return Results.BadRequest(new { error = "Nombre requerido" });
            if (!await db.Paises.AnyAsync(x => x.Id == dto.PaisId)) return Results.BadRequest(new { error = "País inexistente" });
            var nombre = dto.Nombre.Trim();
            if (await db.Departamentos.AnyAsync(x => x.PaisId == dto.PaisId && x.Nombre.ToLower() == nombre.ToLower()))
                return Results.Conflict(new { error = "Departamento duplicado en el país" });

            var d = new Departamento { Nombre = nombre, PaisId = dto.PaisId };
            db.Departamentos.Add(d);
            await db.SaveChangesAsync();
            return Results.Created($"/api/departamentos/{d.Id}", d);
        });

        g.MapPut("{id:int}", async (int id, DepartamentoDto dto, AppDbContext db) =>
        {
            var d = await db.Departamentos.FindAsync(id);
            if (d is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(dto.Nombre)) return Results.BadRequest(new { error = "Nombre requerido" });
            if (!await db.Paises.AnyAsync(x => x.Id == dto.PaisId)) return Results.BadRequest(new { error = "País inexistente" });
            var nombre = dto.Nombre.Trim();
            if (await db.Departamentos.AnyAsync(x => x.Id != id && x.PaisId == dto.PaisId && x.Nombre.ToLower() == nombre.ToLower()))
                return Results.Conflict(new { error = "Departamento duplicado en el país" });
            d.Nombre = nombre;
            d.PaisId = dto.PaisId;
            await db.SaveChangesAsync();
            return Results.Ok(d);
        });

        g.MapDelete("{id:int}", async (int id, AppDbContext db) =>
        {
            var d = await db.Departamentos.FindAsync(id);
            if (d is null) return Results.NotFound();
            if (await db.Municipios.AnyAsync(x => x.DepartamentoId == id) ||
                await db.Empresas.AnyAsync(x => x.DepartamentoId == id))
                return Results.Conflict(new { error = "Tiene municipios o empresas asociados" });
            db.Departamentos.Remove(d);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    // ------------------------------------------------------------ Municipios
    private static void MapMunicipios(RouteGroupBuilder g)
    {
        g.MapGet("", async (int? departamentoId, AppDbContext db) =>
        {
            var q = db.Municipios.AsNoTracking().AsQueryable();
            if (departamentoId is not null) q = q.Where(x => x.DepartamentoId == departamentoId);
            return Results.Ok(await q.OrderBy(x => x.Id).ToListAsync());
        });

        g.MapGet("{id:int}", async (int id, AppDbContext db) =>
            await db.Municipios.FindAsync(id) is { } m ? Results.Ok(m) : Results.NotFound());

        g.MapPost("", async (MunicipioDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre)) return Results.BadRequest(new { error = "Nombre requerido" });
            if (!await db.Departamentos.AnyAsync(x => x.Id == dto.DepartamentoId))
                return Results.BadRequest(new { error = "Departamento inexistente" });
            var nombre = dto.Nombre.Trim();
            if (await db.Municipios.AnyAsync(x => x.DepartamentoId == dto.DepartamentoId && x.Nombre.ToLower() == nombre.ToLower()))
                return Results.Conflict(new { error = "Municipio duplicado en el departamento" });

            var m = new Municipio { Nombre = nombre, DepartamentoId = dto.DepartamentoId };
            db.Municipios.Add(m);
            await db.SaveChangesAsync();
            return Results.Created($"/api/municipios/{m.Id}", m);
        });

        g.MapPut("{id:int}", async (int id, MunicipioDto dto, AppDbContext db) =>
        {
            var m = await db.Municipios.FindAsync(id);
            if (m is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(dto.Nombre)) return Results.BadRequest(new { error = "Nombre requerido" });
            if (!await db.Departamentos.AnyAsync(x => x.Id == dto.DepartamentoId))
                return Results.BadRequest(new { error = "Departamento inexistente" });
            var nombre = dto.Nombre.Trim();
            if (await db.Municipios.AnyAsync(x => x.Id != id && x.DepartamentoId == dto.DepartamentoId && x.Nombre.ToLower() == nombre.ToLower()))
                return Results.Conflict(new { error = "Municipio duplicado en el departamento" });
            m.Nombre = nombre;
            m.DepartamentoId = dto.DepartamentoId;
            await db.SaveChangesAsync();
            return Results.Ok(m);
        });

        g.MapDelete("{id:int}", async (int id, AppDbContext db) =>
        {
            var m = await db.Municipios.FindAsync(id);
            if (m is null) return Results.NotFound();
            if (await db.Empresas.AnyAsync(x => x.MunicipioId == id))
                return Results.Conflict(new { error = "Tiene empresas asociadas" });
            db.Municipios.Remove(m);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
