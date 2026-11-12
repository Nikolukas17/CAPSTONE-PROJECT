# Correcciones puntuales

- `SalonesController`: las reservas de grupos y sesiones se cargan primero con `Include` y se aplanan en memoria. Esto evita que Entity Framework Core genere `SQL APPLY`, operación que SQLite no soporta. Se aplica en consulta, edición y guardado de salones.
- `AsignaturaFormViewModel`: los códigos de asignatura y los códigos compartidos aceptan letras y números, por ejemplo `MA02` y `MEA02`. El código principal admite entre 2 y 15 caracteres y se normaliza a mayúsculas.
- `AsignaturasController`: la validación de código único se aplica tanto al crear como al editar, excluyendo el registro actual. También se normalizan las listas recibidas del formulario para evitar valores nulos.
- Vista de asignaturas: se muestra un resumen de validación para que los errores del formulario sean visibles y no parezca que el guardado queda detenido.

No se cambia la estructura general ni se agrega una migración nueva para estas correcciones.
