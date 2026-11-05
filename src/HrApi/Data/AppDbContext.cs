using HrApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HrApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Pais> Paises => Set<Pais>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Municipio> Municipios => Set<Municipio>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();
    public DbSet<ColaboradorEmpresa> ColaboradorEmpresas => Set<ColaboradorEmpresa>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Departamento>().HasOne<Pais>().WithMany()
            .HasForeignKey(x => x.PaisId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Municipio>().HasOne<Departamento>().WithMany()
            .HasForeignKey(x => x.DepartamentoId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Empresa>(e =>
        {
            e.HasOne<Pais>().WithMany().HasForeignKey(x => x.PaisId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Departamento>().WithMany().HasForeignKey(x => x.DepartamentoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Municipio>().WithMany().HasForeignKey(x => x.MunicipioId).OnDelete(DeleteBehavior.Restrict);
            // DEF-001 (sembrado): intencionalmente SIN índice único sobre Nit.
        });

        mb.Entity<Colaborador>().HasMany(c => c.Empresas).WithOne()
            .HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<ColaboradorEmpresa>().HasOne<Empresa>().WithMany()
            .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        // DEF-007 (sembrado): sin índice único (ColaboradorId, EmpresaId).
    }
}
