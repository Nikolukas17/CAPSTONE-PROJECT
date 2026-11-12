# Integración de persistencia y pregrado

## Ejecutar

Requiere .NET SDK 10:

```bash
dotnet restore
dotnet build
dotnet run
```

La primera ejecución crea `App_Data/oferta_academica.db`, aplica las migraciones y carga los datos iniciales. No elimines esa carpeta si deseas conservar los cambios. Haz copias de seguridad periódicas del archivo SQLite.

## Registro

Crea una cuenta desde **Regístrate** usando un correo `@urosario.edu.co`. El sistema guarda nombre, cargo, correo y hash de contraseña. No existe una contraseña maestra ni se precargan usuarios reales.

## Recuperar contraseña

Configura SMTP siguiendo `CONFIGURACION_PRODUCCION.md`. Sin SMTP configurado, la solicitud no podrá enviar el mensaje y el formulario mostrará una advertencia. No se simula un envío exitoso.

## Planificación

La importación se limita a la hoja **Pregrado** de `Modelo Planificación Oferta.xlsx`: 91 asignaturas/filas válidas. Los valores completos se conservan en `PlanificacionPregrado` como JSON por fila. La generación de oferta utiliza la frecuencia y duración de sesión para proponer los bloques, y la cantidad de grupos y cupo estimado para cada asignatura. Posgrado no se importa.

Los 20 profesores de cátedra son datos sintéticos para pruebas. Sus horarios y materias asignadas son simulaciones, no disponibilidad ni experiencia verificada de personas reales.

## Alcance de validación

Este paquete fue preparado a nivel de código y validación estructural de JSON. No fue posible ejecutar `dotnet build` en el entorno de preparación porque no cuenta con .NET SDK. Antes de usarlo en operación, compila y prueba registro, inicio, recuperación SMTP, edición de catálogos y generación de una proyección con una copia de la base de datos.
