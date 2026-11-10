using System.Globalization;
using System.Text;

namespace OfertaAcademica.Web.Data;

/// <summary>
/// Columna oculta (shadow property) con los campos buscables ya normalizados
/// (mayúsculas, sin tildes) para poder filtrar en SQL sin traer la tabla a memoria.
/// </summary>
public static class TextoBusqueda
{
    public const string Columna = "TextoBusqueda";

    public static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in descompuesto)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string Componer(params string[] campos) =>
        string.Join("|", campos.Select(Normalizar));
}
