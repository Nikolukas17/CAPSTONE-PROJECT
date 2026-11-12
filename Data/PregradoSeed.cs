using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Data;

public static class PregradoSeed
{
    public static void Initialize(AppDbContext db)
    {
        // Normaliza dominios heredados de datos ficticios; no crea credenciales de acceso.
        foreach (var teacher in db.Profesores.Where(p => p.Correo.EndsWith("@example.invalid")))
            teacher.Correo = teacher.Correo.Replace("@example.invalid", "@urosario.edu.co", StringComparison.OrdinalIgnoreCase);
        foreach (var elective in db.Asignaturas.Where(a => a.Tipologia == Tipologia.Electiva && a.Semestre != "6+"))
            elective.Semestre = "6+";

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "PlanificacionPregrado.json");
        if (File.Exists(path) && !db.PlanificacionPregrado.Any())
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var name = item.TryGetProperty("nom_asignatura", out var n) ? n.GetString() ?? "" : "";
                db.PlanificacionPregrado.Add(new PlanificacionPregrado { NombreAsignatura = name, DatosJson = item.GetRawText() });
            }
        }

        // Veinte perfiles de cátedra expresamente simulados, con franjas variadas.
        var courses = db.Asignaturas.AsNoTracking().OrderBy(a => a.Id).ToList();
        for (var i = 1; i <= 20; i++)
        {
            var email = $"profesor.catedra{i:00}@urosario.edu.co";
            if (db.Profesores.Any(p => p.Correo == email)) continue;
            var availability = DisponibilidadSemanal.Vacia();
            var mode = (i - 1) % 4; // mañana, tarde, noche o jornada amplia
            for (var day = 0; day < 6; day++)
                for (var slot = 0; slot < 24; slot++)
                {
                    var hour = 7 + slot / 2;
                    var allowed = mode switch
                    {
                        0 => hour < 12,
                        1 => hour >= 12 && hour < 17,
                        2 => hour >= 17 && hour < 19,
                        _ => true
                    };
                    if (allowed) availability[DisponibilidadSemanal.Indice(slot, day)] = EstadoFranja.Disponible;
                }
            var assigned = courses.Where((_, index) => index % 20 == i - 1 || index % 20 == (i % 20)).ToList();
            var subjectNames = assigned.Select(a => a.Nombre).Distinct().ToList();
            var focus = InferirArea(subjectNames);
            db.Profesores.Add(new Profesor
            {
                Nombre = $"Profesor(a) de Cátedra Simulado {i:00}", Correo = email,
                Tipo = "Cátedra simulado", CursosMinimos = 2, CursosMaximos = 4,
                HorasMinimas = 2, HorasMaximas = 16,
                Perfil = $"Perfil académico simulado de {focus}. Asignaturas de referencia: {string.Join(", ", subjectNames.Take(5))}. Validar experiencia y asignaturas con la coordinación antes de una asignación real.",
                Materias = assigned.Select(a => new MateriaProfesor(a.Codigo, a.Semestre ?? "N/A", a.Nombre)).DistinctBy(m => m.Codigo).ToList(),
                Disponibilidad = availability, Activo = true
            });
        }
        db.SaveChanges();
    }

    private static string InferirArea(IEnumerable<string> materias)
    {
        var texto = string.Join(" ", materias).ToUpperInvariant();
        if (texto.Contains("CALCULO") || texto.Contains("ALGEBRA") || texto.Contains("MATEM") || texto.Contains("ESTAD")) return "matemáticas y análisis cuantitativo";
        if (texto.Contains("PROGRAM") || texto.Contains("DATOS") || texto.Contains("SOFTWARE") || texto.Contains("COMPUT")) return "computación y desarrollo de software";
        if (texto.Contains("FISICA") || texto.Contains("CIRCUITO") || texto.Contains("ELECTR")) return "ciencias físicas e ingeniería";
        return "ingeniería y ciencias aplicadas";
    }
}
