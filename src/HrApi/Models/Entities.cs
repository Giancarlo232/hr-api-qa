namespace HrApi.Models;

public class Pais
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

public class Departamento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int PaisId { get; set; }
}

public class Municipio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int DepartamentoId { get; set; }
}

public class Empresa
{
    public int Id { get; set; }
    public string Nit { get; set; } = "";
    public string RazonSocial { get; set; } = "";
    public string NombreComercial { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Email { get; set; } = "";
    public int PaisId { get; set; }
    public int DepartamentoId { get; set; }
    public int MunicipioId { get; set; }
}

public class Colaborador
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public int Edad { get; set; }
    public string Telefono { get; set; } = "";
    public string Email { get; set; } = "";
    public List<ColaboradorEmpresa> Empresas { get; set; } = [];
}

/// <summary>Relación N:M colaborador-empresa. Clave surrogada: no hay índice único (ver DEF-007).</summary>
public class ColaboradorEmpresa
{
    public int Id { get; set; }
    public int ColaboradorId { get; set; }
    public int EmpresaId { get; set; }
}

public record PaisDto(string? Nombre);
public record DepartamentoDto(string? Nombre, int PaisId);
public record MunicipioDto(string? Nombre, int DepartamentoId);
public record EmpresaDto(string? Nit, string? RazonSocial, string? NombreComercial, string? Telefono,
                         string? Email, int PaisId, int DepartamentoId, int MunicipioId);
public record ColaboradorDto(string? NombreCompleto, int Edad, string? Telefono, string? Email, List<int>? EmpresaIds);
