using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Services;

/// <summary>
/// TEMPORAL: datos de ejemplo para las pantallas que aún no tienen base de datos
/// (Profesores, Salones y Oferta Académica). Se elimina cuando existan los repositorios reales.
/// </summary>
public static class DatosDemo
{
    public static readonly List<Salon> Salones =
    [
        new() { Codigo = "L_001", Nombre = "BARDEEN",             Tipo = "Laboratorio", Equipos = 10, Capacidad = 20 },
        new() { Codigo = "L_002", Nombre = "FARADAY",             Tipo = "Laboratorio", Equipos = 8,  Capacidad = 16 },
        new() { Codigo = "L_003", Nombre = "BERNOULLI",           Tipo = "Laboratorio", Equipos = 0,  Capacidad = 10 },
        new() { Codigo = "L_004", Nombre = "HOPPER",              Tipo = "Laboratorio", Equipos = 20, Capacidad = 20 },
        new() { Codigo = "L_005", Nombre = "ENERGÍAS RENOVABLES", Tipo = "Laboratorio", Equipos = 2,  Capacidad = 6 },
        new() { Codigo = "S_001", Nombre = "BOOLE",               Tipo = "Sala",        Equipos = 24, Capacidad = 24 },
        new() { Codigo = "S_002", Nombre = "HIPATIA",             Tipo = "Sala",        Equipos = 0,  Capacidad = 35 },
        new() { Codigo = "S_003", Nombre = "LOVELACE",            Tipo = "Sala",        Equipos = 30, Capacidad = 30 },
        new() { Codigo = "S_004", Nombre = "TURING",              Tipo = "Sala",        Equipos = 27, Capacidad = 27 },
        new() { Codigo = "S_005", Nombre = "NEUMANN",             Tipo = "Sala",        Equipos = 0,  Capacidad = 40 },
        new() { Codigo = "S_006", Nombre = "306 CASUR",           Tipo = "Sala",        Equipos = 0,  Capacidad = 36 },
        new() { Codigo = "S_007", Nombre = "208 CASUR",           Tipo = "Sala",        Equipos = 28, Capacidad = 32 },
    ];

    public static readonly List<Profesor> Profesores = CrearProfesores();

    private static List<Profesor> CrearProfesores()
    {
        var materiasAndrade = new List<MateriaProfesor>
        {
            new("11310003", "N/A", "Cálculo I"),
            new("11310004", "N/A", "Cálculo II"),
            new("11320003", "11310036", "Cálculo III"),
            new("11310010", "N/A", "Análisis Real"),
            new("11310020", "N/A", "Topología"),
            new("11310018", "11320009", "Álgebra Abstracta"),
        };

        var nombres = new (string Nombre, string Correo, string Tipo)[]
        {
            ("David Eugenio Andrade",         "david.andrade",     "Docencia"),
            ("Nicolás Guillermo Avilán Vargas","nicolas.avilan",    "Docencia"),
            ("Norma Constanza Sarmiento",     "norma.sarmiento",   "Carrera"),
            ("Daniel Alfonso Bojacá",         "daniel.bojaca",     "Carrera"),
            ("José Alejandro Guerrero",       "jose.guerrero",     "Docencia"),
            ("David Nicolás Castiblanco",     "david.castiblanco", "Carrera"),
            ("Marcela Patricia Rojas",        "marcela.rojas",     "Docencia"),
            ("Camilo Ernesto Vargas",         "camilo.vargas",     "Carrera"),
            ("Laura Cristina Pardo",          "laura.pardo",       "Docencia"),
            ("Andrés Mauricio Neira",         "andres.neira",      "Docencia"),
            ("Sofía Elena Quintero",          "sofia.quintero",    "Carrera"),
            ("Julián Esteban Ortiz",          "julian.ortiz",      "Docencia"),
        };

        var lista = new List<Profesor>();
        for (var i = 0; i < nombres.Length; i++)
        {
            var (nombre, correo, tipo) = nombres[i];
            lista.Add(new Profesor
            {
                Id = i + 1,
                Nombre = nombre,
                Correo = $"{correo}@urosario.edu.co",
                Tipo = tipo,
                Telefono = $"+57 300 123 {4567 + i * 11}",
                Perfil = i == 0
                    ? "Doctorado en Matemáticas con énfasis en Análisis y Álgebra, Magíster en Ciencias Matemáticas y Licenciatura en Matemáticas."
                    : "Profesor de la Escuela de Ciencias e Ingeniería.",
                Materias = i == 0 ? materiasAndrade : [],
            });
        }
        return lista;
    }

    // ---------- Oferta académica (semana de ejemplo) ----------

    public record ClaseDemo(
        int Dia, int Hora, string Codigo, int Grupo, string Nombre, string Salon,
        string Profesor, string Horario, int Creditos, int Inscritos, int Cupos, string Programa, int AsignaturaId);

    private static readonly string[] Dias = ["Lun", "Mar", "Mié", "Jue", "Vie"];

    /// <summary>Cada franja (día, hora) tiene una clase principal y N clases extra ("+N asignaturas").</summary>
    public static List<ClaseDemo> ClasesSemana()
    {
        // (día 0-4, hora inicio, id asignatura, salón, extras)
        var principales = new (int Dia, int Hora, int AsigId, string Salon, int Extras)[]
        {
            (0, 7,  14, "206 Casur",       3),
            (0, 9,  1,  "Salón Hipatia",   4),
            (1, 9,  18, "202 Torre 2",     3),
            (2, 9,  5,  "306 Casur",       2),
            (3, 11, 4,  "305 Casur",       1),
            (4, 9,  21, "607 Torre 1",     3),
            (0, 15, 20, "Lab. Bernoulli",  2),
            (2, 15, 20, "Lab. Bernoulli",  2),
        };

        var profesores = Profesores;
        var servicio = new AsignaturaServiceEnMemoria();
        var catalogo = servicio.Buscar(null, 1, 100).Items;
        var clases = new List<ClaseDemo>();
        var rotacion = 0;

        foreach (var p in principales)
        {
            var total = p.Extras + 1;
            for (var k = 0; k < total; k++)
            {
                var asig = k == 0 ? catalogo.First(a => a.Id == p.AsigId) : catalogo[(p.AsigId + k * 3) % catalogo.Count];
                var prof = profesores[(rotacion++) % profesores.Count];
                var salon = k == 0 ? p.Salon : Salones[(rotacion + k) % Salones.Count].Nombre;
                var dias = p.Dia % 2 == 0 ? "Lun y Mié" : "Mar y Jue";
                clases.Add(new ClaseDemo(
                    p.Dia, p.Hora, asig.Codigo, 1 + k % 2, Capitalizar(asig.Nombre), salon, prof.Nombre,
                    $"{dias} {p.Hora}:00 – {p.Hora + 2}:00", asig.Creditos, 20 + (rotacion * 3) % 15, 35,
                    asig.ProgramaCodigo, asig.Id));
            }
        }
        return clases;
    }

    public static string NombreDia(int dia) => Dias[dia];

    private static string Capitalizar(string texto)
    {
        var palabras = texto.ToLowerInvariant().Split(' ');
        var menores = new HashSet<string> { "y", "de", "del", "la", "en", "a" };
        for (var i = 0; i < palabras.Length; i++)
            if (i == 0 || !menores.Contains(palabras[i]))
                palabras[i] = char.ToUpperInvariant(palabras[i][0]) + palabras[i][1..];
        return string.Join(' ', palabras);
    }
}
