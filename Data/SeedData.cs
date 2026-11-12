using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Data;

public static class SeedData
{
    private sealed class ImportData
    {
        public List<ProgramaImport> Programs { get; set; } = [];
        public List<CursoImport> Courses { get; set; } = [];
        public List<ProfesorImport> Teachers { get; set; } = [];
        public List<SalonImport> Rooms { get; set; } = [];
        public List<RelacionImport> Relations { get; set; } = [];
        public Dictionary<string, string> RoomMap { get; set; } = [];
    }
    private sealed class ProgramaImport { public string Code { get; set; } = ""; public string Name { get; set; } = ""; }
    private sealed class CursoImport
    {
        public string Program { get; set; } = ""; public string Code { get; set; } = "";
        public string Name { get; set; } = ""; public string? Semester { get; set; } public string SharedCode { get; set; } = "";
        public string Typology { get; set; } = "T"; public int Credits { get; set; }
        public int Hours { get; set; } public int Theory { get; set; } public int Practice { get; set; } public int ComputersRequired { get; set; }
    }
    private sealed class ProfesorImport { public string Name { get; set; } = ""; public string Category { get; set; } = ""; public List<string> Subjects { get; set; } = []; }
    private sealed class SalonImport { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = ""; public int Computers { get; set; } public int Capacity { get; set; } }
    private sealed class RelacionImport { public string Program { get; set; } = ""; public string Code { get; set; } = ""; public string Related { get; set; } = ""; public string Type { get; set; } = ""; }

    public static void Initialize(AppDbContext db)
    {
        if (db.Programas.Any()) return;
        var ruta = Path.Combine(AppContext.BaseDirectory, "Data", "ImportacionExcel.json");
        if (!File.Exists(ruta))
            throw new FileNotFoundException("No se encontró Data/ImportacionExcel.json. Verifica que el archivo se copie al directorio de salida.", ruta);

        var datos = JsonSerializer.Deserialize<ImportData>(File.ReadAllText(ruta, Encoding.UTF8),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("El archivo de importación no tiene un formato válido.");

        db.Programas.AddRange(datos.Programs.Select(p => new Programa { Codigo = p.Code, Nombre = p.Name }));
        var mapaSalones = datos.RoomMap.ToDictionary(k => Normalizar(k.Key), v => v.Value);
        var entidades = new Dictionary<(string Programa, string Codigo), Asignatura>();
        var id = 1;
        foreach (var grupoCursos in datos.Courses.GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase))
        {
            var c = grupoCursos.First();
            var tipo = c.Typology.Trim().ToUpperInvariant() switch
            {
                "T" => Tipologia.Obligatoria,
                "C" => Tipologia.Complementaria,
                _ => Tipologia.Electiva
            };
            mapaSalones.TryGetValue(Normalizar(c.Name), out var ubicacion);
            var sugeridos = (ubicacion ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var pideSalas = sugeridos.Any(x => Normalizar(x).Contains("SALAS"));
            var pideLaboratorios = sugeridos.Any(x => Normalizar(x).Contains("LABORATORIO"));
            var tipoSalon = pideSalas && pideLaboratorios ? "Cualquiera" :
                pideSalas ? "SALAS" : pideLaboratorios ? "LABORATORIO" : "Cualquiera";
            var ubicacionNormalizada = Normalizar(ubicacion);
            var permitidos = sugeridos.Where(x => !Normalizar(x).Contains("SALA") && !Normalizar(x).Contains("LABORATORIO"))
                .Select(x => datos.Rooms.FirstOrDefault(r => Normalizar(r.Name).Replace(" ", "") == Normalizar(x).Replace(" ", ""))?.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            if (ubicacionNormalizada.Contains("LABORATORIOS O HINNT"))
            {
                tipoSalon = "Cualquiera";
                permitidos = datos.Rooms.Where(r => r.Type.Contains("LABORATORIO", StringComparison.OrdinalIgnoreCase) || r.Name.Equals("HINNT", StringComparison.OrdinalIgnoreCase))
                    .Select(r => r.Name).ToList();
            }
            if (ubicacionNormalizada.Contains("SALAS O LABORATORIOS"))
                tipoSalon = "Cualquiera";
            var tipoAsig = Normalizar(c.Name).Contains("FISICA") && Normalizar(c.Name).Contains("LABORATORIO") ? "Física laboratorio" :
                Normalizar(c.Name).Contains("FISICA") ? "Física teórica" :
                Normalizar(c.Name).Contains("LABORATORIO") ? "Laboratorio" :
                c.Practice > 0 ? "Teórico-práctica" : "Teórica";
            var curso = new Asignatura
            {
                Id = id++, Codigo = c.Code, CodigoCompartido = c.SharedCode, Nombre = c.Name, ProgramaCodigo = c.Program,
                ProgramasCodigosCsv = string.Join(",", grupoCursos.Select(x => x.Program).Distinct(StringComparer.OrdinalIgnoreCase)),
                Semestre = tipo == Tipologia.Electiva ? "6+" : c.Semester, Tipologia = tipo, Creditos = c.Credits,
                IntensidadHoraria = Math.Max(1, c.Hours), Activa = true,
                TipoSalonRequerido = tipoSalon, TipoAsignatura = tipoAsig,
                SalonesPermitidosCsv = string.Join(",", permitidos)
            };
            foreach (var cursoPrograma in grupoCursos)
                entidades[(cursoPrograma.Program, cursoPrograma.Code)] = curso;
            db.Asignaturas.Add(curso);
        }

        db.Salones.AddRange(datos.Rooms.Select(r => new Salon
        {
            Codigo = r.Code, Nombre = r.Name, Tipo = r.Type,
            Equipos = r.Computers, Capacidad = r.Capacity,
            Disponibilidad = DisponibilidadSemanal.Base(), Activo = true
        }));

        var profesores = new List<Profesor>();
        var profId = 1;
        foreach (var t in datos.Teachers)
        {
            var category = t.Category.Trim();
            var doctorado = category.Contains("Doctor", StringComparison.OrdinalIgnoreCase);
            var carrera = category.Equals("Carrera", StringComparison.OrdinalIgnoreCase);
            var subjects = t.Subjects.Select(Normalizar).ToHashSet();
            var materias = datos.Courses.Where(c => subjects.Contains(Normalizar(c.Name)))
                .Select(c => new MateriaProfesor(c.Code, "N/A", c.Name))
                .DistinctBy(m => m.Codigo).ToList();
            var profesor = new Profesor
            {
                Id = profId++, Nombre = t.Name,
                Correo = $"{Normalizar(t.Name).Replace(" ", ".")}@urosario.edu.co",
                Tipo = doctorado ? "Doctorado" : carrera ? "Carrera" : "Docencia",
                CursosMinimos = doctorado ? 0 : carrera ? 2 : 4,
                CursosMaximos = doctorado ? 99 : carrera ? 4 : 5,
                HorasMinimas = doctorado ? 6 : 0,
                HorasMaximas = carrera ? 12 : 200,
                Perfil = doctorado ? "Profesor con doctorado" : $"Profesor de {category}",
                Activo = true, Materias = materias,
                Disponibilidad = DisponibilidadSemanal.Base()
            };
            profesores.Add(profesor);
        }
        db.Profesores.AddRange(profesores);
        db.SaveChanges();

        var relacionesAgregadas = new HashSet<(int Asignatura, int Relacionada, TipoRelacionAsignatura Tipo)>();
        foreach (var r in datos.Relations)
        {
            if (!entidades.TryGetValue((r.Program, r.Code), out var a) ||
                !entidades.TryGetValue((r.Program, r.Related), out var b) || a.Id == b.Id) continue;
            var tipoRelacion = r.Type.Equals("Correquisito", StringComparison.OrdinalIgnoreCase)
                ? TipoRelacionAsignatura.Correquisito : TipoRelacionAsignatura.Prerrequisito;
            if (!relacionesAgregadas.Add((a.Id, b.Id, tipoRelacion))) continue;
            db.RelacionesAsignatura.Add(new RelacionAsignatura
            {
                AsignaturaId = a.Id, RelacionadaId = b.Id, Tipo = tipoRelacion
            });
        }
        db.SaveChanges();
    }

    private static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "";
        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in descompuesto)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return Regex.Replace(sb.ToString().ToUpperInvariant(), @"[^A-Z0-9]+", " ").Trim();
    }
}
