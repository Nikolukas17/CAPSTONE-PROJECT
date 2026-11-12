using Microsoft.EntityFrameworkCore;
using OfertaAcademica.Web.Data;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Services;

public class SalonServiceEF(AppDbContext db) : ISalonService
{
    public (IReadOnlyList<Salon> Items, int Total) Buscar(string? texto, int pagina, int tamanoPagina)
    {
        var consulta = db.Salones.AsNoTracking().Where(s => s.Activo);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = TextoBusqueda.Normalizar(texto);
            consulta = consulta.Where(s => EF.Property<string>(s, TextoBusqueda.Columna).Contains(t));
        }

        var total = consulta.Count();
        var items = consulta
            .OrderBy(s => s.Codigo)
            .Skip((Math.Max(pagina, 1) - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        return (items, total);
    }

    public Salon? Obtener(string codigo) =>
        db.Salones.AsNoTracking().FirstOrDefault(s => s.Codigo == codigo && s.Activo);

    public bool ExisteCodigo(string codigo) =>
        db.Salones.Any(s => s.Activo && s.Codigo == codigo.Trim());

    public Salon Guardar(Salon salon)
    {
        var existente = db.Salones.Find(salon.Codigo);
        if (existente is null)
        {
            db.Salones.Add(salon);
            db.SaveChanges();
            return salon;
        }

        var disponibilidad = salon.Disponibilidad.ToList();
        for (var i = 0; i < disponibilidad.Count && i < existente.Disponibilidad.Count; i++)
            if (disponibilidad[i] == EstadoFranja.Ocupado)
                disponibilidad[i] = existente.Disponibilidad[i];
        salon.Disponibilidad = disponibilidad;
        db.Entry(existente).CurrentValues.SetValues(salon);
        db.SaveChanges();
        return existente;
    }

    public bool Inactivar(string codigo)
    {
        var s = db.Salones.Find(codigo);
        if (s is null) return false;
        s.Activo = false;
        db.SaveChanges();
        return true;
    }
}
