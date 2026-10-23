using Microsoft.AspNetCore.Mvc;
using OfertaAcademica.Web.ViewModels.Oferta;

namespace OfertaAcademica.Web.Controllers;

public class OfertaAcademicaController : Controller
{
    // Lunes de la semana 1 del semestre (ajústalo al calendario real).
    private static readonly DateTime InicioSemestre = new(2026, 8, 3);
    private const int TotalSemanas = 16;

    public IActionResult Index(int? semana)
    {
        var n = Math.Clamp(semana ?? 1, 1, TotalSemanas);

        var vm = new OfertaAcademicaViewModel
        {
            Semana = n,
            TotalSemanas = TotalSemanas,
            InicioSemana = InicioSemestre.AddDays((n - 1) * 7),
            Sesiones = DatosDemo()
        };
        return View(vm);
    }

    // TODO: reemplazar por los datos reales (servicio de asignaturas / oferta).
    private static List<SesionClaseVM> DatosDemo()
    {
        var id = 0;
        SesionClaseVM S(int asigId, string cod, string nombre, string prof, string salon, string grupo,
                        DayOfWeek dia, int ini, int fin, string color, int cupo = 30) => new()
        {
            Id = ++id, AsignaturaId = asigId, Codigo = cod, Nombre = nombre, Profesor = prof, Salon = salon,
            Grupo = grupo, Dia = dia, Inicio = new TimeSpan(ini, 0, 0), Fin = new TimeSpan(fin, 0, 0),
            Color = color, Programa = "MAC2", Cupo = cupo
        };

        const DayOfWeek L = DayOfWeek.Monday, M = DayOfWeek.Tuesday, X = DayOfWeek.Wednesday,
                        J = DayOfWeek.Thursday, V = DayOfWeek.Friday, Sa = DayOfWeek.Saturday;

        return new()
        {
            // Lunes 7-9: 4 asignaturas en la misma franja
            S(1, "11320005", "Variable Compleja", "David Eugenio Andrade", "206 Casur", "1", L, 7, 9, "rojo", 35),
            S(2, "11320010", "Geometría", "Nicolás Guillermo Avilán Vargas", "312 Casur", "1", L, 7, 9, "verde"),
            S(3, "11310004", "Cálculo 2", "Norma Constanza Sarmiento", "501 Casur", "2", L, 7, 9, "azul"),
            S(4, "11310021", "Análisis Numérico", "José Alejandro Guerrero", "Salón Lovelace", "1", L, 7, 9, "amarillo"),

            S(5, "10000002233", "Algoritmos y Estructura de Datos", "Camila Hipatia Rojas", "Salón Hipatia", "1", L, 9, 11, "verde"),
            S(6, "10000004812", "Fundamentos de Sistemas Digitales", "Andrés Felipe Mora", "Lab. Boole", "1", L, 9, 11, "azul"),
            S(7, "10024483210", "Álgebra Lineal", "Camilo Pérez", "105 Casur", "2", L, 9, 11, "amarillo"),

            S(8, "10005320100", "Laboratorio de Física", "Laura Gómez", "Lab. Bernoulli", "1", L, 15, 17, "gris"),
            S(9, "10100000123", "Optimización", "Juan Pablo Díaz", "208 Casur", "1", L, 15, 17, "gris"),

            S(10, "10000002233", "Algoritmos y Estructura de Datos", "Camila Hipatia Rojas", "Salón Hipatia", "1", M, 9, 11, "verde"),
            S(11, "11146000000", "Microeconomía", "Sofía Herrera", "310 Casur", "1", M, 13, 15, "rosa"),

            S(12, "11320005", "Variable Compleja", "David Eugenio Andrade", "206 Casur", "1", X, 7, 9, "rojo", 35),
            S(13, "11310004", "Cálculo 2", "Norma Constanza Sarmiento", "501 Casur", "2", X, 7, 9, "azul"),
            S(14, "10005320100", "Control", "Mateo Ruiz", "Lab. Bernoulli", "1", X, 11, 13, "gris"),

            S(15, "10000002233", "Algoritmos y Estructura de Datos", "Camila Hipatia Rojas", "Salón Hipatia", "1", J, 9, 11, "verde"),
            S(16, "11146000000", "Microeconomía", "Sofía Herrera", "310 Casur", "1", J, 13, 15, "rosa"),

            S(17, "10005320100", "Laboratorio de Física", "Laura Gómez", "Lab. Bernoulli", "1", V, 15, 17, "gris"),
            S(18, "11310021", "Análisis Numérico", "José Alejandro Guerrero", "Salón Lovelace", "1", V, 7, 9, "amarillo"),

            S(19, "10100000123", "Optimización", "Juan Pablo Díaz", "208 Casur", "1", Sa, 9, 11, "azul"),
        };
    }
}
