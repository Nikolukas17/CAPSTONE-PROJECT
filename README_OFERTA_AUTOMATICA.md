# Oferta académica automática – instrucciones

## Instalación y ejecución

1. Haz una copia de seguridad de `oferta_academica.db` si ya tienes información registrada.
2. Abre en Visual Studio Code la carpeta interior `CAPSTONE-PROJECT-main`, donde está `OfertaAcademica.Web.csproj`.
3. En una terminal ejecuta:

   ```powershell
   dotnet restore
   dotnet build
   dotnet run
   ```

4. Al iniciar, EF Core aplica las migraciones pendientes. La nueva migración incorpora disponibilidad de salones, restricciones de asignaturas y límites docentes. En una base vacía, `Data/ImportacionExcel.json` carga el catálogo de los cinco programas, profesores, salones y relaciones académicas.

Se requiere .NET 10.

## Datos importados desde los Excel

- Programas: MA02, MA03, ISE1, IIN1 e IET1.
- Asignaturas: archivos de pensum de cada programa; se conserva código, programa, nombre, tipología y semestre.
- Prerrequisitos y correquisitos: hoja correspondiente del archivo de relaciones por plan.
- Profesores y categorías: archivo `Profesores EICT pregrados 2024-1.xlsx`. Como el archivo no trae correos, se crean direcciones `.invalid` de marcador para que el dato pueda completarse; no son correos reales.
- Asignaturas autorizadas por profesor: matriz `Asig_Docente` del archivo `Modelo Planificación Oferta.xlsx`.
- Salones: hoja `Capacidad espacios` del archivo `Ubicación asignatura de acuerdo a requerimientos.xlsx`.
- Restricciones de espacio por asignatura: hoja `Espacio x asignatura`.

La intensidad semanal se toma de `Modelo Planificación Oferta.xlsx` cuando hay coincidencia por nombre. Cuando no existe esa coincidencia, se usa como aproximación la suma de créditos teóricos y prácticos del pensum; conviene validar esos casos con la coordinación académica.

## Disponibilidad y generación

La cuadrícula usa 24 intervalos de 30 minutos por día, de lunes a sábado, entre las 7:00 y las 19:00. La disponibilidad se administra por separado para salones, profesores y asignaturas. En el detalle del salón se visualizan las franjas libres y las ocupadas en las proyecciones guardadas.

En **Oferta Académica → Generar oferta**, selecciona las asignaturas, grupos y cupos. El generador asigna profesores, salones y horarios en incrementos de 30 minutos. Cada grupo puede tener hasta cinco sesiones semanales, en días diferentes.

## Restricciones consideradas por el generador

- Un profesor y un salón no pueden tener dos sesiones simultáneas.
- Un profesor solo puede dictar asignaturas asociadas en su ficha; también se compara el código compartido.
- Se respeta la disponibilidad semanal de asignatura, profesor y salón.
- Las asignaturas obligatorias del mismo programa y semestre no pueden cruzarse.
- Los correquisitos incluidos no pueden programarse simultáneamente; si falta un correquisito, se informa una advertencia. Los prerrequisitos se conservan como relación académica y no se fuerzan a coincidir en el horario.
- La capacidad del salón debe cubrir el cupo solicitado y se aplican los máximos por tipo: teórica 40, teórico-práctica 25, laboratorio 20–40, física teórica 45 y física laboratorio 15.
- Las asignaturas de computación/programación requieren un salón con al menos un computador; también se respetan los tipos y nombres de salón sugeridos en la hoja de ubicación.
- Profesor de carrera: mínimo 2 y máximo 4 cursos, máximo 12 horas semanales. Profesor de docencia: 4 a 5 cursos y sin tope horario configurado. Profesor con doctorado: mínimo 6 horas. Los mínimos que no puedan alcanzarse se reportan como advertencias; los máximos sí bloquean asignaciones adicionales.

Al editar un salón, el formulario muestra las reservas de proyecciones como franjas ocupadas. No permite reducir su capacidad por debajo de un cupo reservado ni bloquear una franja reservada. El estado ocupado es solo visual y no se guarda como indisponibilidad permanente.

## Conservación de la interfaz y exportación

Se conserva la estructura general de navegación y páginas. Se amplían los controles existentes para editar restricciones y se adapta la cuadrícula/calendario a intervalos de media hora. El botón **Generar Excel** conserva su función y exporta los grupos programados con profesor, salón y horarios por día.

## Consideraciones importantes

- El generador es heurístico con retroceso; busca una solución factible, pero no garantiza el óptimo global. Si faltan profesores habilitados, salones compatibles o franjas suficientes, la proyección queda parcial y explica los motivos.
- El archivo de importación se aplica únicamente cuando la base de datos está vacía. Si ya existe un catálogo en `oferta_academica.db`, la aplicación no lo reemplaza automáticamente; haz copia de seguridad y utiliza una base vacía para probar la carga inicial de estos Excel.
- La hoja de profesores contiene categorías Carrera y Docencia; no se encontraron profesores clasificados explícitamente como Doctorado en ese archivo. La categoría Doctorado queda disponible para registrar manualmente.
- La intensidad semanal y los requisitos de salón derivados por nombre deben validarse con coordinación antes de usar la oferta como programación institucional definitiva.

## Validación técnica

No fue posible ejecutar `dotnet build` en el entorno de preparación porque no está instalado el SDK de .NET. Después de descargar el proyecto, ejecuta `dotnet restore` y `dotnet build` antes de ponerlo en producción.
