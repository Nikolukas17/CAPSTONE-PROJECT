using System.Globalization;
using System.Text;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Services;

/// <summary>TEMPORAL: datos de ejemplo en memoria mientras no hay base de datos.</summary>
public class AsignaturaServiceEnMemoria : IAsignaturaService
{
    private readonly object _candado = new();
    private readonly List<Asignatura> _datos;
    private int _siguienteId;

    private static readonly List<Programa> Programas =
    [
        new() { Codigo = "MACC", Nombre = "Matemáticas Aplicadas y Ciencias de la Computación" },
        new() { Codigo = "MA02", Nombre = "Matemáticas" },
        new() { Codigo = "IS01", Nombre = "Ingeniería de Sistemas" },
        new() { Codigo = "IM01", Nombre = "Ingeniería Matemática" },
    ];

    public AsignaturaServiceEnMemoria()
    {
        _datos = Semilla();
        _siguienteId = _datos.Max(a => a.Id) + 1;
    }

    public (IReadOnlyList<Asignatura> Items, int Total) Buscar(string? texto, int pagina, int tamanoPagina)
    {
        lock (_candado)
        {
            var consulta = _datos.Where(a => a.Activa);

            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = Normalizar(texto);
                consulta = consulta.Where(a =>
                    a.Codigo.Contains(t) ||
                    a.CodigoCompartido.Contains(t) ||
                    Normalizar(a.Nombre).Contains(t));
            }

            var lista = consulta.OrderBy(a => a.Id).ToList();
            var items = lista.Skip((Math.Max(pagina, 1) - 1) * tamanoPagina).Take(tamanoPagina).ToList();
            return (items, lista.Count);
        }
    }

    public Asignatura? Obtener(int id)
    {
        lock (_candado) return _datos.FirstOrDefault(a => a.Id == id && a.Activa);
    }

    public bool ExisteCodigo(string codigo, int idExcluido = 0)
    {
        lock (_candado)
            return _datos.Any(a => a.Activa && a.Id != idExcluido &&
                                   string.Equals(a.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public Asignatura Guardar(Asignatura asignatura)
    {
        lock (_candado)
        {
            if (asignatura.Id == 0)
            {
                asignatura.Id = _siguienteId++;
                _datos.Add(asignatura);
            }
            else
            {
                var indice = _datos.FindIndex(a => a.Id == asignatura.Id);
                if (indice >= 0) _datos[indice] = asignatura;
            }
            return asignatura;
        }
    }

    public bool Inactivar(int id)
    {
        lock (_candado)
        {
            var asignatura = _datos.FirstOrDefault(a => a.Id == id);
            if (asignatura is null) return false;
            asignatura.Activa = false;
            return true;
        }
    }

    public IReadOnlyList<Programa> ObtenerProgramas() => Programas;

    // ---------- utilidades ----------

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in descompuesto)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static List<Asignatura> Semilla()
    {
        // (código, código compartido, nombre, tipología, programa, créditos, horas/semana)
        var filas = new (string, string, string, Tipologia, string, int, int)[]
        {
            ("10000002233", "38000002233",  "ALGORITMOS Y ESTRUCTURA DE DATOS",   Tipologia.Obligatoria,    "MA02", 3, 5),
            ("10000004812", "380078253721", "FUNDAMENTOS DE SISTEMAS DIGITALES",  Tipologia.Obligatoria,    "MACC", 3, 4),
            ("10000000001", "390210001232", "COMPUTACIÓN EN LA NUBE",             Tipologia.Complementaria, "MACC", 3, 4),
            ("10100000123", "382120000001", "OPTIMIZACIÓN",                       Tipologia.Obligatoria,    "MACC", 3, 4),
            ("10024453210", "380004567887", "ÁLGEBRA LINEAL",                     Tipologia.Obligatoria,    "MA02", 3, 4),
            ("10005320100", "380038239112", "CONTROL",                            Tipologia.Obligatoria,    "IS01", 1, 2),
            ("11148300000", "384567876543", "MICROECONOMÍA",                      Tipologia.Obligatoria,    "IM01", 2, 3),
            ("11310003",    "",             "CÁLCULO I",                          Tipologia.Obligatoria,    "MA02", 4, 6),
            ("11310004",    "",             "CÁLCULO II",                         Tipologia.Obligatoria,    "MA02", 4, 6),
            ("11320003",    "11310036",     "CÁLCULO III",                        Tipologia.Obligatoria,    "MA02", 4, 6),
            ("11310010",    "",             "ANÁLISIS REAL",                      Tipologia.Obligatoria,    "MA02", 3, 4),
            ("11310020",    "",             "TOPOLOGÍA",                          Tipologia.Obligatoria,    "MA02", 3, 4),
            ("11310018",    "11320009",     "ÁLGEBRA ABSTRACTA",                  Tipologia.Obligatoria,    "MA02", 3, 4),
            ("11320005",    "",             "VARIABLE COMPLEJA",                  Tipologia.Obligatoria,    "MACC", 3, 4),
            ("11320010",    "",             "GEOMETRÍA",                          Tipologia.Electiva,       "MA02", 3, 4),
            ("11310021",    "",             "ANÁLISIS NUMÉRICO",                  Tipologia.Obligatoria,    "MACC", 3, 4),
            ("11330001",    "",             "TEORÍA DE SISTEMAS",                 Tipologia.Obligatoria,    "IS01", 3, 4),
            ("11340001",    "",             "FÍSICA I",                           Tipologia.Obligatoria,    "IM01", 3, 5),
            ("11340002",    "",             "FÍSICA II",                          Tipologia.Obligatoria,    "IM01", 3, 5),
            ("11340003",    "",             "LABORATORIO DE FÍSICA",              Tipologia.Obligatoria,    "IM01", 1, 2),
            ("11350001",    "",             "INGLÉS IV",                          Tipologia.Complementaria, "MACC", 2, 4),
            ("11360001",    "",             "PROBABILIDAD Y ESTADÍSTICA",         Tipologia.Obligatoria,    "MACC", 3, 4),
            ("11360002",    "",             "BASES DE DATOS",                     Tipologia.Obligatoria,    "IS01", 3, 4),
            ("11360003",    "",             "INTELIGENCIA ARTIFICIAL",            Tipologia.Electiva,       "MACC", 3, 4),
            ("11360004",    "",             "APRENDIZAJE DE MÁQUINA",             Tipologia.Electiva,       "MACC", 3, 4),
            ("11360005",    "",             "ECUACIONES DIFERENCIALES",           Tipologia.Obligatoria,    "MA02", 3, 4),
            ("11360006",    "",             "SEGURIDAD INFORMÁTICA",              Tipologia.Complementaria, "IS01", 3, 4),
            ("11360007",    "",             "INGENIERÍA DE SOFTWARE",             Tipologia.Obligatoria,    "IS01", 3, 5),
        };

        var lista = new List<Asignatura>();
        for (var i = 0; i < filas.Length; i++)
        {
            var (codigo, compartido, nombre, tipologia, programa, creditos, horas) = filas[i];
            var id = i + 1;

            var disponibilidad = DisponibilidadSemanal.Base();
            // Franjas ya asignadas en la oferta (solo lectura en la interfaz).
            var franja = id % 3;
            foreach (var dia in new[] { 0, 2, 4 })
                if (id % 2 == 1) disponibilidad[DisponibilidadSemanal.Indice(franja, dia)] = EstadoFranja.Ocupado;

            lista.Add(new Asignatura
            {
                Id = id,
                Codigo = codigo,
                CodigoCompartido = string.IsNullOrEmpty(compartido) ? "38" + codigo[2..] : compartido,
                Nombre = nombre,
                Tipologia = tipologia,
                ProgramaCodigo = programa,
                Creditos = creditos,
                IntensidadHoraria = horas,
                Disponibilidad = disponibilidad
            });
        }
        return lista;
    }
}
