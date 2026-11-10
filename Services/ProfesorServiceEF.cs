using Microsoft.EntityFrameworkCore;
using OfertaAcademica.Web.Data;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Services;

public class ProfesorServiceEF(AppDbContext db) : IProfesorService
{
    public (IReadOnlyList<Profesor> Items, int Total) Buscar(string? texto, int pagina, int tamanoPagina)
    {
        var consulta = db.Profesores.AsNoTracking().Where(p => p.Activo);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = TextoBusqueda.Normalizar(texto);
            consulta = consulta.Where(p => EF.Property<string>(p, TextoBusqueda.Columna).Contains(t));
        }

        var total = consulta.Count();
        var items = consulta
            .OrderBy(p => p.Nombre)
            .Skip((Math.Max(pagina, 1) - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        return (items, total);
    }

    public Profesor? Obtener(int id) =>
        db.Profesores.AsNoTracking().FirstOrDefault(p => p.Id == id && p.Activo);

    public bool ExisteCorreo(string correo, int idExcluido = 0)
    {
        var c = correo.Trim().ToUpperInvariant();
        return db.Profesores.Any(p => p.Activo && p.Id != idExcluido && p.Correo.ToUpper() == c);
    }

    public Profesor Guardar(Profesor profesor)
    {
        var existente = profesor.Id == 0 ? null : db.Profesores.FirstOrDefault(p => p.Id == profesor.Id);
        if (existente is null)
        {
            db.Profesores.Add(profesor);
            db.SaveChanges();
            return profesor;
        }

        // Cargar la entidad rastreada y reemplazar la colección hace que EF
        // borre las filas de ProfesorMaterias que ya no están e inserte las nuevas.
        db.Entry(existente).CurrentValues.SetValues(profesor);
        existente.Materias = profesor.Materias;
        db.SaveChanges();
        return existente;
    }

    public bool Inactivar(int id)
    {
        var p = db.Profesores.Find(id);
        if (p is null) return false;
        p.Activo = false;
        db.SaveChanges();
        return true;
    }
}
