using System.Text.RegularExpressions;

namespace OfertaAcademica.Web.Services;

/// <summary>Una asignatura con grupos sin programar por una misma causa.</summary>
public sealed record PendienteAsignatura(string Codigo, string Nombre, int Grupos, string Detalle);

/// <summary>Causa por la que quedaron grupos sin programar, con sugerencia de cómo resolverla.</summary>
public sealed record CategoriaPendiente(string Clave, string Titulo, string Icono, string Ayuda, List<PendienteAsignatura> Asignaturas)
{
    public int TotalGrupos => Asignaturas.Sum(a => a.Grupos);
}

/// <summary>Aviso que no impide programar grupos (p. ej. un profesor por debajo de su carga mínima).</summary>
public sealed record AvisoProyeccion(string Titulo, List<string> Detalles);

public sealed record ResumenProyeccion(List<CategoriaPendiente> Pendientes, List<AvisoProyeccion> Avisos)
{
    public int GruposPendientes => Pendientes.Sum(c => c.TotalGrupos);

    /// <summary>
    /// Agrupa el mensaje línea a línea de <see cref="GeneradorOfertaService"/> por causa y asignatura,
    /// para mostrarlo resumido. Las líneas que no reconoce (p. ej. proyecciones antiguas) van a "Otros avisos".
    /// </summary>
    public static ResumenProyeccion Desde(string? mensaje)
    {
        var definiciones = new (string Clave, string Titulo, string Icono, string Ayuda, Regex Patron, Func<Match, string> Detalle)[]
        {
            ("salon", "Sin salón con capacidad suficiente", "bi-door-closed",
                "Los salones exigidos son más pequeños que el cupo. Divide la asignatura en más grupos o ajusta sus salones permitidos.",
                new Regex(@"^(?<cod>\S+) - (?<nom>.+?): no hay salones compatibles con un cupo de (?<cupo>\d+) \((?:salones requeridos: )?(?<det>.+?)\)\.?$"),
                m => $"Cupo {m.Groups["cupo"].Value} · {m.Groups["det"].Value}"),
            ("cupo", "Cupo por encima de la regla de capacidad", "bi-people",
                "El cupo por grupo supera el máximo permitido para su tipo. Aumenta la cantidad de grupos en la planeación.",
                new Regex(@"^(?<cod>\S+?)(?: - (?<nom>.+?))?: el cupo (?<cupo>\d+) incumple la regla de capacidad para (?<tipo>.+?) \(permitido: (?<perm>.+)\)\.?$"),
                m => $"Cupo {m.Groups["cupo"].Value} · {m.Groups["tipo"].Value}, {m.Groups["perm"].Value}"),
            ("profesor", "Sin profesor disponible", "bi-person-x",
                "Sus profesores no tienen disponibilidad registrada o ya llegaron a su carga máxima. Registra la disponibilidad de cátedra o asocia más profesores a la asignatura.",
                new Regex(@"^(?<cod>\S+) - (?<nom>.+?): sus profesores no tienen disponibilidad registrada o ya alcanzaron su carga máxima\.?$"),
                _ => "Disponibilidad o carga máxima"),
            ("sin-profesor", "Sin profesores asociados", "bi-person-dash",
                "Ningún profesor activo tiene esta asignatura en su ficha. Asócialos desde la ficha del profesor.",
                new Regex(@"^(?<cod>\S+) - (?<nom>.+?): no tiene profesores activos asociados en su ficha\.?$"),
                _ => "Sin profesores en la ficha"),
            ("horario", "Sin horario compatible", "bi-calendar-x",
                "Hay profesores y salones, pero sus horarios libres no coinciden. Amplía la disponibilidad o asocia otro profesor.",
                new Regex(@"^(?<cod>\S+) - (?<nom>.+?), grupo (?<g>\d+): no se encontró una combinación de profesor, salón y horario disponible\.?$"),
                m => m.Groups["g"].Value),
        };

        var pendientes = new Dictionary<string, Dictionary<string, (string Nombre, int Grupos, List<string> Detalles)>>();
        var cargaMinima = new List<string>();
        var otros = new List<string>();
        var avisos = new List<AvisoProyeccion>();
        var regexCarga = new Regex(@"^(?<nom>.+?): quedó con (?<n>\d+) (?<u>cursos|horas); su mínimo configurado es (?<m>\d+)\.?$");
        var regexSinDisp = new Regex(@"^Advertencia: (?<n>\d+) profesor\(es\) no tienen disponibilidad registrada y no se les asignó ningún curso: (?<lista>.+?)\.?$");

        foreach (var linea in (mensaje ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            if (linea == "Todos los grupos solicitados fueron programados.") continue;

            var reconocida = false;
            foreach (var def in definiciones)
            {
                var m = def.Patron.Match(linea);
                if (!m.Success) continue;
                var codigo = m.Groups["cod"].Value;
                var nombre = m.Groups["nom"].Success ? m.Groups["nom"].Value : "";
                if (!pendientes.TryGetValue(def.Clave, out var porAsignatura))
                    pendientes[def.Clave] = porAsignatura = new();
                if (!porAsignatura.TryGetValue(codigo, out var item))
                    item = (nombre, 0, []);
                var detalle = def.Detalle(m);
                if (!item.Detalles.Contains(detalle)) item.Detalles.Add(detalle);
                porAsignatura[codigo] = (item.Nombre.Length > 0 ? item.Nombre : nombre, item.Grupos + 1, item.Detalles);
                reconocida = true;
                break;
            }
            if (reconocida) continue;

            if (regexCarga.Match(linea) is { Success: true } carga)
                cargaMinima.Add($"{carga.Groups["nom"].Value}: {carga.Groups["n"].Value} de {carga.Groups["m"].Value} {carga.Groups["u"].Value} mínimos");
            else if (regexSinDisp.Match(linea) is { Success: true } sinDisp)
                avisos.Add(new($"{sinDisp.Groups["n"].Value} profesor(es) sin disponibilidad registrada (no recibieron cursos)",
                    sinDisp.Groups["lista"].Value.Split(", ").ToList()));
            else
                otros.Add(linea.StartsWith("Advertencia: ") ? linea["Advertencia: ".Length..] : linea);
        }

        if (cargaMinima.Count > 0)
            avisos.Add(new($"{cargaMinima.Count} profesor(es) por debajo de su carga mínima", cargaMinima));
        if (otros.Count > 0)
            avisos.Add(new("Otros avisos", otros));

        var categorias = definiciones
            .Where(d => pendientes.ContainsKey(d.Clave))
            .Select(d => new CategoriaPendiente(d.Clave, d.Titulo, d.Icono, d.Ayuda,
                pendientes[d.Clave]
                    .Select(kv => new PendienteAsignatura(kv.Key, kv.Value.Nombre, kv.Value.Grupos,
                        d.Clave == "horario" ? (kv.Value.Detalles.Count == 1 ? "Grupo " : "Grupos ") + string.Join(", ", kv.Value.Detalles) : kv.Value.Detalles[0]))
                    .OrderByDescending(a => a.Grupos).ThenBy(a => a.Nombre).ToList()))
            .OrderByDescending(c => c.TotalGrupos).ToList();

        return new ResumenProyeccion(categorias, avisos);
    }
}
