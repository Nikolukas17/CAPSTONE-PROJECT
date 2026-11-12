using Microsoft.EntityFrameworkCore;
using OfertaAcademica.Web.Data;
using OfertaAcademica.Web.Models.Entities;
using OfertaAcademica.Web.ViewModels.Oferta;

namespace OfertaAcademica.Web.Services;

public record SolicitudGrupo(int AsignaturaId, int NumeroGrupo, int Cupo);

public class GeneradorOfertaService(AppDbContext db)
{
    private const int InicioDia = 7 * 60;
    private const int FinDia = 19 * 60;
    private const int MinutosBloque = 30;

    private sealed record Colocacion(int ProfesorId, string SalonCodigo, DayOfWeek Dia, int Inicio, int Fin, int AsignaturaId, string Programa, string? Semestre, bool Obligatoria);

    public ProyeccionOferta Generar(string nombre, IReadOnlyList<SolicitudOfertaInput> solicitudes)
    {
        var ids = solicitudes.Select(s => s.AsignaturaId).Distinct().ToList();
        var asignaturas = db.Asignaturas.AsNoTracking().Where(a => ids.Contains(a.Id)).ToDictionary(a => a.Id);
        var profesores = db.Profesores.Include(p => p.Materias).AsNoTracking().Where(p => p.Activo).ToList();
        var salones = db.Salones.AsNoTracking().Where(s => s.Activo).ToList();
        var patronesPregrado = db.PlanificacionPregrado.AsNoTracking().ToList()
            .GroupBy(p => Normalizar(p.NombreAsignatura))
            .ToDictionary(g => g.Key, g => LeerPatronPregrado(g.First().DatosJson));
        var proyeccion = new ProyeccionOferta
        {
            Nombre = nombre.Trim(), FechaCreacion = DateTime.UtcNow, EsActual = true,
            Solicitudes = solicitudes.Where(s => asignaturas.ContainsKey(s.AsignaturaId))
                .GroupBy(s => s.AsignaturaId).Select(g => g.Last()).Select(s => new SolicitudOferta
                {
                    AsignaturaId = s.AsignaturaId, Incluir = s.Incluir,
                    CantidadGrupos = s.CantidadGrupos, Cupo = s.Cupo
                }).ToList()
        };
        var ocupadas = new List<Colocacion>();
        var cursosDocente = new HashSet<(int ProfesorId, int AsignaturaId)>();
        var horasDocente = new Dictionary<int, int>();
        var seleccionadas = solicitudes.Where(s => s.Incluir).ToList();
        var idsSeleccionados = seleccionadas.Select(s => s.AsignaturaId).ToHashSet();
        var relacionesCorrequisito = db.RelacionesAsignatura.AsNoTracking()
            .Where(r => ids.Contains(r.AsignaturaId) && r.Tipo == TipoRelacionAsignatura.Correquisito)
            .ToList();
        var paresCorrequisito = relacionesCorrequisito
            .Select(r => r.AsignaturaId < r.RelacionadaId ? (r.AsignaturaId, r.RelacionadaId) : (r.RelacionadaId, r.AsignaturaId))
            .ToHashSet();
        var errores = new List<string>();
        foreach (var relacion in relacionesCorrequisito.Where(r => idsSeleccionados.Contains(r.AsignaturaId) && !idsSeleccionados.Contains(r.RelacionadaId)))
        {
            var nombreAsignatura = asignaturas.TryGetValue(relacion.AsignaturaId, out var asig) ? asig.Nombre : relacion.AsignaturaId.ToString();
            errores.Add($"Advertencia: el correquisito asociado a '{nombreAsignatura}' no está incluido en esta proyección.");
        }
        foreach (var solicitud in seleccionadas.Where(s => !asignaturas.ContainsKey(s.AsignaturaId)))
            errores.Add($"La asignatura con identificador {solicitud.AsignaturaId} ya no existe.");
        foreach (var solicitud in seleccionadas.Where(s => asignaturas.ContainsKey(s.AsignaturaId) && !asignaturas[s.AsignaturaId].Activa))
            errores.Add($"{asignaturas[solicitud.AsignaturaId].Codigo}: la asignatura está inactiva y no se puede programar.");

        var pendientes = seleccionadas
            .Where(s => asignaturas.ContainsKey(s.AsignaturaId) && asignaturas[s.AsignaturaId].Activa)
            .SelectMany(s => Enumerable.Range(1, s.CantidadGrupos).Select(n => new SolicitudGrupo(s.AsignaturaId, n, s.Cupo)))
            .OrderBy(s => profesores.Count(p => PuedeDictar(p, asignaturas[s.AsignaturaId])))
            .ThenByDescending(s => asignaturas[s.AsignaturaId].IntensidadHoraria)
            .ToList();

        proyeccion.TotalGruposSolicitados = seleccionadas.Sum(s => Math.Max(0, s.CantidadGrupos));

        foreach (var solicitud in pendientes)
        {
            var asignatura = asignaturas[solicitud.AsignaturaId];
            var docentes = profesores.Where(p => PuedeDictar(p, asignatura) &&
                cursosDocente.Count(x => x.ProfesorId == p.Id) < p.CursosMaximos &&
                horasDocente.GetValueOrDefault(p.Id) + asignatura.IntensidadHoraria <= p.HorasMaximas)
                .OrderBy(p => cursosDocente.Count(x => x.ProfesorId == p.Id))
                .ThenBy(p => horasDocente.GetValueOrDefault(p.Id))
                .ThenBy(p => p.Nombre).ToList();
            if (docentes.Count == 0)
            {
                errores.Add($"{asignatura.Codigo} - {asignatura.Nombre}: no tiene profesores activos asociados en su ficha.");
                continue;
            }

            var cupoMaximo = asignatura.TipoAsignatura switch
            {
                "Teórica" => 40,
                "Teórico-práctica" => 25,
                "Laboratorio" => 40,
                "Física teórica" => 45,
                "Física laboratorio" => 15,
                _ => 500
            };
            if (solicitud.Cupo > cupoMaximo ||
                (asignatura.TipoAsignatura == "Laboratorio" && solicitud.Cupo < 20))
            {
                errores.Add($"{asignatura.Codigo}: el cupo {solicitud.Cupo} incumple la regla de capacidad para {asignatura.TipoAsignatura} (permitido: {(asignatura.TipoAsignatura == "Laboratorio" ? "20 a 40" : $"máximo {cupoMaximo}")}).");
                continue;
            }

            var nombresPermitidos = asignatura.SalonesPermitidosCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var salonesValidos = salones.Where(s =>
                s.Capacidad >= solicitud.Cupo &&
                (asignatura.TipoSalonRequerido == "Cualquiera" ||
                 string.Equals(s.Tipo, asignatura.TipoSalonRequerido, StringComparison.OrdinalIgnoreCase)) &&
                (nombresPermitidos.Length == 0 || nombresPermitidos.Any(n => string.Equals(n, s.Nombre, StringComparison.OrdinalIgnoreCase))) &&
                (!asignatura.Nombre.Contains("PROGRAMACIÓN", StringComparison.OrdinalIgnoreCase) &&
                 !asignatura.Nombre.Contains("COMPUTACIÓN", StringComparison.OrdinalIgnoreCase) ||
                 s.Equipos > 0)).ToList();
            if (asignatura.TipoAsignatura.Contains("Laboratorio", StringComparison.OrdinalIgnoreCase) ||
                asignatura.Nombre.Contains("LABORATORIO", StringComparison.OrdinalIgnoreCase))
                salonesValidos = salonesValidos.Where(s => s.Tipo.Contains("laboratorio", StringComparison.OrdinalIgnoreCase)).ToList();
            else
                salonesValidos = salonesValidos.OrderBy(s => s.Tipo.Contains("sala", StringComparison.OrdinalIgnoreCase) ? 0 : 1).ToList();

            if (salonesValidos.Count == 0)
            {
                errores.Add($"{asignatura.Codigo} - {asignatura.Nombre}: no hay salones compatibles con un cupo de {solicitud.Cupo}.");
                continue;
            }

            var programado = false;
            var disponibilidadesGrupo = LeerDisponibilidades(asignatura);
            var disponibilidadGrupo = solicitud.NumeroGrupo <= disponibilidadesGrupo.Count
                ? disponibilidadesGrupo[solicitud.NumeroGrupo - 1] : asignatura.Disponibilidad;

            foreach (var docente in docentes)
            {
                foreach (var salon in salonesValidos)
                {
                    foreach (var patron in ObtenerPatrones(asignatura.IntensidadHoraria, patronesPregrado.GetValueOrDefault(Normalizar(asignatura.Nombre))))
                    {
                        var sesiones = new List<Colocacion>();
                        if (!UbicarSesiones(asignatura, disponibilidadGrupo, docente, salon, patron, solicitud.NumeroGrupo, 0, sesiones, ocupadas, paresCorrequisito))
                            continue;

                        var grupo = new GrupoOferta
                        {
                            Proyeccion = proyeccion,
                            AsignaturaId = asignatura.Id,
                            ProfesorId = docente.Id,
                            SalonCodigo = salon.Codigo,
                            NumeroGrupo = solicitud.NumeroGrupo,
                            Cupo = solicitud.Cupo,
                            Sesiones = sesiones.Select(c => new SesionOferta
                            {
                                Dia = c.Dia,
                                InicioMinutos = c.Inicio,
                                FinMinutos = c.Fin
                            }).ToList()
                        };
                        proyeccion.Grupos.Add(grupo);
                        ocupadas.AddRange(sesiones);
                        cursosDocente.Add((docente.Id, asignatura.Id));
                        horasDocente[docente.Id] = horasDocente.GetValueOrDefault(docente.Id) + asignatura.IntensidadHoraria;
                        programado = true;
                        break;
                    }
                    if (programado) break;
                }
                if (programado) break;
            }

            if (!programado)
                errores.Add($"{asignatura.Codigo} - {asignatura.Nombre}, grupo {solicitud.NumeroGrupo}: no se encontró una combinación de profesor, salón y horario disponible.");
        }

        foreach (var docente in profesores)
        {
            var cursos = cursosDocente.Count(x => x.ProfesorId == docente.Id);
            var horas = horasDocente.GetValueOrDefault(docente.Id);
            if (cursos > 0 && cursos < docente.CursosMinimos)
                errores.Add($"{docente.Nombre}: quedó con {cursos} cursos; su mínimo configurado es {docente.CursosMinimos}.");
            if (horas > 0 && horas < docente.HorasMinimas)
                errores.Add($"{docente.Nombre}: quedó con {horas} horas; su mínimo configurado es {docente.HorasMinimas}.");
        }

        proyeccion.TotalGruposProgramados = proyeccion.Grupos.Count;
        proyeccion.Estado = proyeccion.TotalGruposProgramados == proyeccion.TotalGruposSolicitados
            ? errores.Count == 0 ? "Completa" : "Completa con advertencias"
            : proyeccion.TotalGruposProgramados == 0 ? "Sin solución" : "Parcial";
        proyeccion.Mensaje = errores.Count == 0
            ? "Todos los grupos solicitados fueron programados."
            : string.Join(Environment.NewLine, errores);

        foreach (var anterior in db.ProyeccionesOferta.Where(p => p.EsActual))
            anterior.EsActual = false;
        db.ProyeccionesOferta.Add(proyeccion);
        db.SaveChanges();
        return proyeccion;
    }

    public ProyeccionOferta? RecalcularProyeccionActual()
    {
        var anterior = db.ProyeccionesOferta.AsNoTracking()
            .Include(p => p.Solicitudes)
            .Where(p => p.EsActual)
            .OrderByDescending(p => p.FechaCreacion)
            .FirstOrDefault();
        if (anterior is null) return null;

        var entradas = anterior.Solicitudes.Select(s => new SolicitudOfertaInput(
            s.AsignaturaId, s.Incluir, s.CantidadGrupos, s.Cupo)).ToList();
        var existentes = entradas.Select(s => s.AsignaturaId).ToHashSet();
        var nuevas = db.Asignaturas.AsNoTracking().Where(a => a.Activa && !existentes.Contains(a.Id)).ToList();
        entradas.AddRange(nuevas.Select(a => new SolicitudOfertaInput(a.Id, true, a.CantidadGrupos, 30)));

        return Generar($"{anterior.Nombre} · actualización {DateTime.Now:dd/MM HH:mm}", entradas);
    }

    private static List<List<EstadoFranja>> LeerDisponibilidades(Asignatura asignatura)
    {
        try
        {
            var grupos = System.Text.Json.JsonSerializer.Deserialize<List<List<EstadoFranja>>>(asignatura.DisponibilidadesGruposJson);
            if (grupos is { Count: > 0 }) return grupos;
        }
        catch { }
        return Enumerable.Range(0, Math.Max(1, asignatura.CantidadGrupos))
            .Select(_ => asignatura.Disponibilidad.ToList()).ToList();
    }

    private static bool PuedeDictar(Profesor profesor, Asignatura asignatura) =>
        profesor.Materias.Any(m =>
            string.Equals(m.Codigo, asignatura.Codigo, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(asignatura.CodigoCompartido) && string.Equals(m.Codigo, asignatura.CodigoCompartido, StringComparison.OrdinalIgnoreCase)));

    private static int[]? LeerPatronPregrado(string datosJson)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(datosJson);
            var root = doc.RootElement;
            var sesiones = new List<int>();
            for (var i = 1; i <= 4; i++)
            {
                if (!root.TryGetProperty($"frecuencia_sesion_{i}", out var frecuencia) ||
                    !root.TryGetProperty($"horas_sesion_{i}", out var horas)) continue;
                var veces = frecuencia.ValueKind == System.Text.Json.JsonValueKind.Number ? (int)Math.Round(frecuencia.GetDouble()) : 0;
                var duracion = horas.ValueKind == System.Text.Json.JsonValueKind.Number ? horas.GetDouble() : 0;
                var bloques = (int)Math.Round(duracion * 2);
                if (veces <= 0 || bloques < 2) continue;
                for (var j = 0; j < veces && sesiones.Count < 5; j++) sesiones.Add(bloques);
            }
            return sesiones.Count > 0 ? sesiones.ToArray() : null;
        }
        catch (System.Text.Json.JsonException) { return null; }
    }

    private static string Normalizar(string value)
    {
        var decomposed = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = decomposed.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Trim().ToUpperInvariant();
    }

    private static List<int[]> ObtenerPatrones(int horas, int[]? patronPregrado = null)
    {
        if (patronPregrado is { Length: > 0 }) return [patronPregrado];
        var unidades = horas * 2;
        var patrones = new List<int[]>();
        Construir(unidades, 0, [], patrones);
        return patrones
            .Where(p => p.Length <= 5)
            .OrderBy(p => unidades <= 4 ? (p.Length == 1 ? 0 : 1) : (p.Length == 1 ? 1 : 0))
            .ThenBy(p => p.Max() - p.Min())
            .ThenBy(p => p.Length)
            .ToList();

        static void Construir(int restante, int minimo, List<int> actual, List<int[]> resultado)
        {
            if (restante == 0)
            {
                if (actual.Count > 0) resultado.Add(actual.ToArray());
                return;
            }
            if (actual.Count == 5) return;
            for (var siguiente = Math.Min(8, restante); siguiente >= Math.Max(2, minimo); siguiente--)
            {
                actual.Add(siguiente);
                Construir(restante - siguiente, siguiente, actual, resultado);
                actual.RemoveAt(actual.Count - 1);
            }
        }
    }

    private static bool UbicarSesiones(Asignatura asignatura, List<EstadoFranja> disponibilidadGrupo, Profesor profesor, Salon salon, int[] patron,
        int numeroGrupo, int indice, List<Colocacion> provisionales, List<Colocacion> ocupadas, HashSet<(int, int)> paresCorrequisito)
    {
        if (indice == patron.Length) return true;
        var duracion = patron[indice] * MinutosBloque;
        var esCapstone = asignatura.Nombre.Contains("CAPSTONE", StringComparison.OrdinalIgnoreCase);
        var dias = esCapstone && numeroGrupo == 2
            ? new List<DayOfWeek> { DayOfWeek.Saturday }
            : patron.Length == 2
                ? new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }
                : patron.Length == 3
                    ? new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Tuesday, DayOfWeek.Thursday }
                    : new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        dias = dias.OrderBy(d => provisionales.Any(p => p.Dia == d) ? 1 : 0).ThenBy(d => dias.IndexOf(d)).ToList();

        foreach (var dia in dias)
        {
            if (provisionales.Any(p => p.Dia == dia)) continue;
            for (var inicio = InicioDia; inicio + duracion <= FinDia; inicio += MinutosBloque)
            {
                var fin = inicio + duracion;
                if (!Disponible(disponibilidadGrupo, dia, inicio, fin) ||
                    !Disponible(profesor.Disponibilidad, dia, inicio, fin) ||
                    !Disponible(salon.Disponibilidad, dia, inicio, fin))
                    continue;

                var candidata = new Colocacion(profesor.Id, salon.Codigo, dia, inicio, fin, asignatura.Id, asignatura.ProgramasCodigosCsv.Length > 0 ? asignatura.ProgramasCodigosCsv : asignatura.ProgramaCodigo, asignatura.Semestre, asignatura.Tipologia == Tipologia.Obligatoria);
                if (Conflicta(candidata, ocupadas, paresCorrequisito) || Conflicta(candidata, provisionales, paresCorrequisito)) continue;

                provisionales.Add(candidata);
                if (UbicarSesiones(asignatura, disponibilidadGrupo, profesor, salon, patron, numeroGrupo, indice + 1, provisionales, ocupadas, paresCorrequisito)) return true;
                provisionales.RemoveAt(provisionales.Count - 1);
            }
        }
        return false;
    }

    private static bool Disponible(List<EstadoFranja> disponibilidad, DayOfWeek dia, int inicio, int fin)
    {
        if (disponibilidad.Count != DisponibilidadSemanal.Total) return false;
        var diaIndex = (int)dia - 1;
        for (var minuto = inicio; minuto < fin; minuto += MinutosBloque)
        {
            var franja = (minuto - InicioDia) / MinutosBloque;
            var indice = DisponibilidadSemanal.Indice(franja, diaIndex);
            if (indice < 0 || indice >= disponibilidad.Count || disponibilidad[indice] != EstadoFranja.Disponible)
                return false;
        }
        return true;
    }

    private static bool CompartenPrograma(string a, string b)
    {
        var programasA = a.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var programasB = b.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return programasA.Intersect(programasB, StringComparer.OrdinalIgnoreCase).Any();
    }

    private static bool Conflicta(Colocacion candidata, IEnumerable<Colocacion> existentes, HashSet<(int, int)> paresCorrequisito)
    {
        foreach (var otra in existentes)
        {
            if (otra.Dia != candidata.Dia || candidata.Inicio >= otra.Fin || otra.Inicio >= candidata.Fin) continue;
            if (otra.ProfesorId == candidata.ProfesorId || otra.SalonCodigo == candidata.SalonCodigo) return true;
            if (otra.AsignaturaId == candidata.AsignaturaId) continue; // Los grupos paralelos son alternativas.
            var par = otra.AsignaturaId < candidata.AsignaturaId
                ? (otra.AsignaturaId, candidata.AsignaturaId)
                : (candidata.AsignaturaId, otra.AsignaturaId);
            if (paresCorrequisito.Contains(par)) return true;
            if (otra.Obligatoria && candidata.Obligatoria &&
                CompartenPrograma(otra.Programa, candidata.Programa) &&
                otra.Semestre == candidata.Semestre &&
                !string.IsNullOrWhiteSpace(candidata.Semestre))
                return true;
        }
        return false;
    }
}
