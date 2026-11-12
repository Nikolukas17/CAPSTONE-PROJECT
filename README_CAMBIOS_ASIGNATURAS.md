# Cambios de asignaturas compartidas

Se conservaron la estructura MVC, las vistas y los módulos existentes del proyecto.

## Cambios incluidos
- Una asignatura se identifica por un código único global, no por la combinación código-programa.
- El formulario de asignaturas permite seleccionar varios programas para un mismo registro.
- La importación inicial agrupa los cursos que comparten código y los asocia a un solo registro.
- La lista y el selector de asignaturas para profesores muestran una sola entrada por código.
- Se quitó de la asignatura el campo de computadores mínimos. La compatibilidad se determina con las características de los salones.
- Cada asignatura permite configurar la cantidad de grupos y una disponibilidad semanal independiente para cada grupo.
- El generador de oferta usa la disponibilidad correspondiente al número de grupo y toma la cantidad de grupos configurada en la asignatura como valor inicial.
- Se conserva la asociación de asignaturas de profesores por código.

## Base de datos
Se agregó la migración `20261110050000_AsignaturasCompartidasYGrupos`. El proyecto ejecuta `Database.Migrate()` al iniciar, por lo que la migración se aplica automáticamente al arrancar la aplicación.

La migración incorpora la lista de programas, la cantidad de grupos y las disponibilidades por grupo; además elimina de la tabla de asignaturas el campo de computadores mínimos. En los registros existentes se conserva el programa anterior y se agrupan los códigos de programa de registros que compartían código, tomando como registro principal el de menor identificador.

## Ejecución
Requiere el SDK de .NET 10 y las dependencias NuGet declaradas en `OfertaAcademica.Web.csproj`. Abre la carpeta del proyecto y ejecuta:

```bash
dotnet restore
dotnet run
```

La compilación no se pudo ejecutar en el entorno de preparación porque no está instalado el SDK de .NET. Por eso, antes de desplegar, ejecuta `dotnet build` en un entorno con .NET 10 y verifica la migración sobre una copia de la base de datos.
