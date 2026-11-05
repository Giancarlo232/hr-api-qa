using HrApi.Data;
using HrApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HrApi.Endpoints;

public static class ColaboradoresEndpoints
{
    public static void MapColaboradores(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/colaboradores");

        g.MapGet("", async (string? nombre, AppDbContext db) =>
        {
            // DEF-008 (sembrado): sin paginación y con filtro en memoria (carga toda la tabla).
            var todos = await db.Colaboradores.Include(c => c.Empresas).ToListAsync();
            var filtrados = string.IsNullOrWhiteSpace(nombre)
                ? todos
                : todos.Where(c => c.NombreCompleto.Contains(nombre, StringComparison.OrdinalIgnoreCase)).ToList();
            return Results.Ok(filtrados.Select(Map));
        });

        g.MapGet("{id:int}", async (int id, AppDbContext db) =>
        {
            var c = await db.Colaboradores.Include(x => x.Empresas).FirstOrDefaultAsync(x => x.Id == id);
            // DEF-006 (sembrado): si no existe devuelve 200 con cuerpo nulo en lugar de 404.
            return Results.Ok(c is null ? null : Map(c));
        });

        g.MapPost("", async (ColaboradorDto dto, AppDbContext db) =>
        {
            var error = await Validar(dto, db);
            if (error is not null) return Results.BadRequest(new { error });

            var c = new Colaborador();
            Aplicar(c, dto);
            db.Colaboradores.Add(c);
            await db.SaveChangesAsync();
            return Results.Created($"/api/colaboradores/{c.Id}", Map(c));
        });

        g.MapPut("{id:int}", async (int id, ColaboradorDto dto, AppDbContext db) =>
        {
            var c = await db.Colaboradores.Include(x => x.Empresas).FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return Results.NotFound();

            var error = await Validar(dto, db);
            if (error is not null) return Results.BadRequest(new { error });

            c.Empresas.Clear();
            Aplicar(c, dto);
            await db.SaveChangesAsync();
            return Results.Ok(Map(c));
        });

        g.MapDelete("{id:int}", async (int id, AppDbContext db) =>
        {
            var c = await db.Colaboradores.FindAsync(id);
            if (c is null) return Results.NotFound();
            db.Colaboradores.Remove(c);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Asociar un colaborador existente a otra empresa.
        g.MapPost("{id:int}/empresas/{empresaId:int}", async (int id, int empresaId, AppDbContext db) =>
        {
            var c = await db.Colaboradores.Include(x => x.Empresas).FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return Results.NotFound();
            if (!await db.Empresas.AnyAsync(x => x.Id == empresaId))
                return Results.BadRequest(new { error = "Empresa inexistente" });

            // DEF-007 (sembrado): no se verifica si la asociación ya existe.
            c.Empresas.Add(new ColaboradorEmpresa { EmpresaId = empresaId });
            await db.SaveChangesAsync();
            return Results.Ok(Map(c));
        });
    }

    private static async Task<string?> Validar(ColaboradorDto d, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(d.NombreCompleto)) return "Nombre completo requerido";
        if (string.IsNullOrWhiteSpace(d.Telefono)) return "Teléfono requerido";
        if (string.IsNullOrWhiteSpace(d.Email)) return "Correo electrónico requerido";
        // DEF-005 (sembrado): no se valida el formato del correo.
        if (d.Edad > 120) return "Edad fuera de rango";
        // DEF-002 (sembrado): solo se controla el máximo; 0 y valores negativos se aceptan.
        if (d.EmpresaIds is null || d.EmpresaIds.Count == 0) return "Debe pertenecer al menos a una empresa";

        var ids = d.EmpresaIds.Distinct().ToList();
        var existentes = await db.Empresas.CountAsync(e => ids.Contains(e.Id));
        if (existentes != ids.Count) return "Alguna empresa no existe";
        return null;
    }

    private static void Aplicar(Colaborador c, ColaboradorDto d)
    {
        c.NombreCompleto = d.NombreCompleto!.Trim();
        c.Edad = d.Edad;
        c.Telefono = d.Telefono!.Trim();
        c.Email = d.Email!.Trim();
        foreach (var id in d.EmpresaIds!.Distinct())
            c.Empresas.Add(new ColaboradorEmpresa { EmpresaId = id });
    }

    private static object Map(Colaborador c) => new
    {
        c.Id,
        c.NombreCompleto,
        c.Edad,
        c.Telefono,
        c.Email,
        EmpresaIds = c.Empresas.Select(e => e.EmpresaId).ToList()
    };
}
