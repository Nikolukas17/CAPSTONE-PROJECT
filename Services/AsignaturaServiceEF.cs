using Microsoft.EntityFrameworkCore;
using OfertaAcademica.Web.Data;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Services;

public class AsignaturaServiceEF(AppDbContext db) : IAsignaturaService
{
    public (IReadOnlyList<Asignatura> Items, int Total) Buscar(string? texto, int pagina, int tamanoPagina)
    {
        var consulta = db.Asignaturas.AsNoTracking().Where(a => a.Activa);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = TextoBusqueda.Normalizar(texto);
            consulta = consulta.Where(a => EF.Property<string>(a, TextoBusqueda.Columna).Contains(t));
        }

        var unicas = consulta.ToList().GroupBy(a => a.Codigo, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(a => a.Id).First()).OrderBy(a => a.Id).ToList();
        var total = unicas.Count;
        var items = unicas
            .Skip((Math.Max(pagina, 1) - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        return (items, total);
    }

    public Asignatura? Obtener(int id) =>
        db.Asignaturas.AsNoTracking().FirstOrDefault(a => a.Id == id && a.Activa);

    public bool ExisteCodigo(string codigo, int idExcluido = 0, string? programaCodigo = null)
    {
        var c = codigo.Trim().ToUpperInvariant();
        return db.Asignaturas.Any(a => a.Activa && a.Id != idExcluido && a.Codigo.ToUpper() == c);
    }

    public Asignatura Guardar(Asignatura asignatura)
    {
        var existente = asignatura.Id == 0 ? null : db.Asignaturas.Find(asignatura.Id);
        if (existente is null)
        {
            db.Asignaturas.Add(asignatura);
            db.SaveChanges();
            return asignatura;
        }

        db.Entry(existente).CurrentValues.SetValues(asignatura);
        db.SaveChanges();
        return existente;
    }

    public bool Inactivar(int id)
    {
        var a = db.Asignaturas.Find(id);
        if (a is null) return false;
        a.Activa = false;
        db.SaveChanges();
        return true;
    }

    public IReadOnlyList<Programa> ObtenerProgramas() =>
        db.Programas.AsNoTracking().OrderBy(p => p.Codigo).ToList();
}
