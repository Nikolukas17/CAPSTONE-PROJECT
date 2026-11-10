using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        if (db.Programas.Any()) return;

        // ---- Programas ----
        db.Programas.AddRange(
            new Programa { Codigo = "MACC", Nombre = "Matemáticas Aplicadas y Ciencias de la Computación" },
            new Programa { Codigo = "MA02", Nombre = "Matemáticas" },
            new Programa { Codigo = "IS01", Nombre = "Ingeniería de Sistemas" },
            new Programa { Codigo = "IM01", Nombre = "Ingeniería Matemática" }
        );

        // ---- Asignaturas ----
        var filasAsig = new (string Codigo, string CodigoCompartido, string Nombre, Tipologia Tipologia, string Programa, int Creditos, int Horas)[]
        {
            ("10000002233", "38000002233",  "ALGORITMOS Y ESTRUCTURA DE DATOS",   Tipologia.Obligatoria,    "MA02", 3, 5),
            ("10000004812", "380078253721", "FUNDAMENTOS DE SISTEMAS DIGITALES",  Tipologia.Obligatoria,    "MACC", 3, 4),
            ("10000000001", "390210001232", "COMPUTACIÓN EN LA NUBE",             Tipologia.Complementaria, "MACC", 3, 4),
            ("10100000123", "382120000001", "OPTIMIZACIÓN",                       Tipologia.Obligatoria,    "MACC", 3, 4),
            ("10024453210", "380004567887", "ÁLGEBRA LINEAL",                     Tipologia.Obligatoria,    "MA02", 3, 4),
            ("10005320100", "380038239112", "CONTROL",                            Tipologia.Obligatoria,    "IS01", 1, 2),
            ("11148300000", "384567876543", "MICROECONOMÍA",                      Tipologia.Obligatoria,    "IM01", 2, 3),
            ("11310003",    "3811310003",   "CÁLCULO I",                          Tipologia.Obligatoria,    "MA02", 4, 6),
            ("11310004",    "3811310004",   "CÁLCULO II",                         Tipologia.Obligatoria,    "MA02", 4, 6),
            ("11320003",    "11310036",     "CÁLCULO III",                        Tipologia.Obligatoria,    "MA02", 4, 6),
            ("11310010",    "3811310010",   "ANÁLISIS REAL",                      Tipologia.Obligatoria,    "MA02", 3, 4),
            ("11310020",    "3811310020",   "TOPOLOGÍA",                          Tipologia.Obligatoria,    "MA02", 3, 4),
            ("11310018",    "11320009",     "ÁLGEBRA ABSTRACTA",                  Tipologia.Obligatoria,    "MA02", 3, 4),
            ("11320005",    "3811320005",   "VARIABLE COMPLEJA",                  Tipologia.Obligatoria,    "MACC", 3, 4),
            ("11320010",    "3811320010",   "GEOMETRÍA",                          Tipologia.Electiva,       "MA02", 3, 4),
            ("11310021",    "3811310021",   "ANÁLISIS NUMÉRICO",                  Tipologia.Obligatoria,    "MACC", 3, 4),
            ("11330001",    "3811330001",   "TEORÍA DE SISTEMAS",                 Tipologia.Obligatoria,    "IS01", 3, 4),
            ("11340001",    "3811340001",   "FÍSICA I",                           Tipologia.Obligatoria,    "IM01", 3, 5),
            ("11340002",    "3811340002",   "FÍSICA II",                          Tipologia.Obligatoria,    "IM01", 3, 5),
            ("11340003",    "3811340003",   "LABORATORIO DE FÍSICA",              Tipologia.Obligatoria,    "IM01", 1, 2),
            ("11350001",    "3811350001",   "INGLÉS IV",                          Tipologia.Complementaria, "MACC", 2, 4),
            ("11360001",    "3811360001",   "PROBABILIDAD Y ESTADÍSTICA",         Tipologia.Obligatoria,    "MACC", 3, 4),
            ("11360002",    "3811360002",   "BASES DE DATOS",                     Tipologia.Obligatoria,    "IS01", 3, 4),
            ("11360003",    "3811360003",   "INTELIGENCIA ARTIFICIAL",            Tipologia.Electiva,       "MACC", 3, 4),
            ("11360004",    "3811360004",   "APRENDIZAJE DE MÁQUINA",             Tipologia.Electiva,       "MACC", 3, 4),
            ("11360005",    "3811360005",   "ECUACIONES DIFERENCIALES",           Tipologia.Obligatoria,    "MA02", 3, 4),
            ("11360006",    "3811360006",   "SEGURIDAD INFORMÁTICA",              Tipologia.Complementaria, "IS01", 3, 4),
            ("11360007",    "3811360007",   "INGENIERÍA DE SOFTWARE",             Tipologia.Obligatoria,    "IS01", 3, 5),
        };

        for (var i = 0; i < filasAsig.Length; i++)
        {
            var (codigo, compartido, nombre, tipologia, programa, creditos, horas) = filasAsig[i];
            var id = i + 1;
            var disp = DisponibilidadSemanal.Base();
            var franja = id % 3;
            foreach (var dia in new[] { 0, 2, 4 })
                if (id % 2 == 1) disp[DisponibilidadSemanal.Indice(franja, dia)] = EstadoFranja.Ocupado;

            db.Asignaturas.Add(new Asignatura
            {
                Id = id,
                Codigo = codigo,
                CodigoCompartido = compartido,
                Nombre = nombre,
                Tipologia = tipologia,
                ProgramaCodigo = programa,
                Creditos = creditos,
                IntensidadHoraria = horas,
                Activa = true,
                Disponibilidad = disp
            });
        }

        // ---- Salones ----
        db.Salones.AddRange(
            new Salon { Codigo = "L_001", Nombre = "BARDEEN",             Tipo = "Laboratorio", Equipos = 10, Capacidad = 20 },
            new Salon { Codigo = "L_002", Nombre = "FARADAY",             Tipo = "Laboratorio", Equipos = 8,  Capacidad = 16 },
            new Salon { Codigo = "L_003", Nombre = "BERNOULLI",           Tipo = "Laboratorio", Equipos = 0,  Capacidad = 10 },
            new Salon { Codigo = "L_004", Nombre = "HOPPER",              Tipo = "Laboratorio", Equipos = 20, Capacidad = 20 },
            new Salon { Codigo = "L_005", Nombre = "ENERGÍAS RENOVABLES", Tipo = "Laboratorio", Equipos = 2,  Capacidad = 6  },
            new Salon { Codigo = "S_001", Nombre = "BOOLE",               Tipo = "Sala",        Equipos = 24, Capacidad = 24 },
            new Salon { Codigo = "S_002", Nombre = "HIPATIA",             Tipo = "Sala",        Equipos = 0,  Capacidad = 35 },
            new Salon { Codigo = "S_003", Nombre = "LOVELACE",            Tipo = "Sala",        Equipos = 30, Capacidad = 30 },
            new Salon { Codigo = "S_004", Nombre = "TURING",              Tipo = "Sala",        Equipos = 27, Capacidad = 27 },
            new Salon { Codigo = "S_005", Nombre = "NEUMANN",             Tipo = "Sala",        Equipos = 0,  Capacidad = 40 },
            new Salon { Codigo = "S_006", Nombre = "306 CASUR",           Tipo = "Sala",        Equipos = 0,  Capacidad = 36 },
            new Salon { Codigo = "S_007", Nombre = "208 CASUR",           Tipo = "Sala",        Equipos = 28, Capacidad = 32 }
        );

        // ---- Profesores ----
        var materiasAndrade = new List<MateriaProfesor>
        {
            new("11310003", "N/A", "Cálculo I"),
            new("11310004", "N/A", "Cálculo II"),
            new("11320003", "11310036", "Cálculo III"),
            new("11310010", "N/A", "Análisis Real"),
            new("11310020", "N/A", "Topología"),
            new("11310018", "11320009", "Álgebra Abstracta"),
        };

        var nombresProf = new (string Nombre, string Correo, string Tipo)[]
        {
            ("David Eugenio Andrade",          "david.andrade",     "Docencia"),
            ("Nicolás Guillermo Avilán Vargas", "nicolas.avilan",    "Docencia"),
            ("Norma Constanza Sarmiento",      "norma.sarmiento",   "Carrera"),
            ("Daniel Alfonso Bojacá",          "daniel.bojaca",     "Carrera"),
            ("José Alejandro Guerrero",        "jose.guerrero",     "Docencia"),
            ("David Nicolás Castiblanco",      "david.castiblanco", "Carrera"),
            ("Marcela Patricia Rojas",         "marcela.rojas",     "Docencia"),
            ("Camilo Ernesto Vargas",          "camilo.vargas",     "Carrera"),
            ("Laura Cristina Pardo",           "laura.pardo",       "Docencia"),
            ("Andrés Mauricio Neira",          "andres.neira",      "Docencia"),
            ("Sofía Elena Quintero",           "sofia.quintero",    "Carrera"),
            ("Julián Esteban Ortiz",           "julian.ortiz",      "Docencia"),
        };

        for (var i = 0; i < nombresProf.Length; i++)
        {
            var (nombre, correo, tipo) = nombresProf[i];
            db.Profesores.Add(new Profesor
            {
                Id = i + 1,
                Nombre = nombre,
                Correo = $"{correo}@urosario.edu.co",
                Tipo = tipo,
                Telefono = $"+57 300 123 {4567 + i * 11}",
                Perfil = i == 0
                    ? "Doctorado en Matemáticas con énfasis en Análisis y Álgebra, Magíster en Ciencias Matemáticas y Licenciatura en Matemáticas."
                    : "Profesor de la Escuela de Ciencias e Ingeniería.",
                Activo = true,
                Materias = i == 0 ? materiasAndrade : [],
                Disponibilidad = DisponibilidadSemanal.Base()
            });
        }

        db.SaveChanges();
    }
}
