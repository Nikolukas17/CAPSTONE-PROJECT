using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Services;

public class ExcelOfertaService
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRel = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";

    public byte[] Generar(ProyeccionOferta proyeccion)
    {
        var encabezados = new List<string>
        {
            "PROGRAMA", "SEMESTRE", "CÓDIGO", "CÓDIGO COMPARTIDO", "NOMBRE ASIGNATURA", "TIPOLOGÍA", "GRUPO", "PROFESOR"
        };
        foreach (var dia in new[] { "LUNES", "MARTES", "MIÉRCOLES", "JUEVES", "VIERNES", "SÁBADO" })
        {
            encabezados.Add(dia + " INICIO");
            encabezados.Add(dia + " FIN");
        }
        encabezados.Add("SALÓN");

        var filas = new List<IReadOnlyList<string>> { encabezados };
        foreach (var grupo in proyeccion.Grupos.OrderBy(g => g.Asignatura!.Codigo).ThenBy(g => g.NumeroGrupo))
        {
            var fila = new List<string>
            {
                (grupo.Asignatura!.ProgramasCodigosCsv.Length > 0 ? grupo.Asignatura.ProgramasCodigosCsv : grupo.Asignatura.ProgramaCodigo),
                grupo.Asignatura.Semestre ?? "ELECTIVA / SIN SEMESTRE FIJO",
                grupo.Asignatura.Codigo,
                grupo.Asignatura.CodigoCompartido,
                grupo.Asignatura.Nombre,
                grupo.Asignatura.Tipologia.ToString(),
                grupo.NumeroGrupo.ToString(CultureInfo.InvariantCulture),
                grupo.Profesor!.Nombre
            };

            foreach (var dia in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday })
            {
                var sesion = grupo.Sesiones.Where(s => s.Dia == dia).OrderBy(s => s.InicioMinutos).FirstOrDefault();
                fila.Add(sesion is null ? "" : Hora(sesion.InicioMinutos));
                fila.Add(sesion is null ? "" : Hora(sesion.FinMinutos));
            }
            fila.Add(grupo.Salon!.Nombre);
            filas.Add(fila);
        }

        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, true))
        {
            Escribir(zip, "[Content_Types].xml", new XDocument(
                new XElement(ContentTypes + "Types",
                    new XElement(ContentTypes + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                    new XElement(ContentTypes + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                    new XElement(ContentTypes + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                    new XElement(ContentTypes + "Override", new XAttribute("PartName", "/xl/worksheets/sheet1.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")))));

            Escribir(zip, "_rels/.rels", new XDocument(
                new XElement(PackageRel + "Relationships",
                    new XElement(PackageRel + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"), new XAttribute("Target", "xl/workbook.xml")))));

            Escribir(zip, "xl/workbook.xml", new XDocument(
                new XElement(Main + "workbook",
                    new XAttribute(XNamespace.Xmlns + "r", Rel),
                    new XElement(Main + "sheets", new XElement(Main + "sheet", new XAttribute("name", "Oferta académica"), new XAttribute("sheetId", 1), new XAttribute(Rel + "id", "rId1"))))));

            Escribir(zip, "xl/_rels/workbook.xml.rels", new XDocument(
                new XElement(PackageRel + "Relationships",
                    new XElement(PackageRel + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"), new XAttribute("Target", "worksheets/sheet1.xml")))));

            var sheetData = new XElement(Main + "sheetData");
            for (var r = 0; r < filas.Count; r++)
            {
                var row = new XElement(Main + "row", new XAttribute("r", r + 1));
                for (var c = 0; c < filas[r].Count; c++)
                {
                    var referencia = Columna(c + 1) + (r + 1).ToString(CultureInfo.InvariantCulture);
                    row.Add(new XElement(Main + "c", new XAttribute("r", referencia), new XAttribute("t", "inlineStr"),
                        new XElement(Main + "is", new XElement(Main + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), filas[r][c] ?? ""))));
                }
                sheetData.Add(row);
            }

            var ultimaColumna = Columna(encabezados.Count);
            var worksheet = new XDocument(new XElement(Main + "worksheet",
                new XElement(Main + "dimension", new XAttribute("ref", $"A1:{ultimaColumna}{filas.Count}")),
                sheetData,
                new XElement(Main + "autoFilter", new XAttribute("ref", $"A1:{ultimaColumna}{filas.Count}"))));
            Escribir(zip, "xl/worksheets/sheet1.xml", worksheet);
        }
        return memoria.ToArray();
    }

    private static string Hora(int minutos) => $"{minutos / 60:00}:{minutos % 60:00}";

    private static string Columna(int numero)
    {
        var resultado = "";
        while (numero > 0)
        {
            numero--;
            resultado = (char)('A' + numero % 26) + resultado;
            numero /= 26;
        }
        return resultado;
    }

    private static void Escribir(ZipArchive zip, string ruta, XDocument documento)
    {
        var entrada = zip.CreateEntry(ruta, CompressionLevel.Optimal);
        using var flujo = entrada.Open();
        documento.Save(flujo);
    }
}
