# Gestión de Oferta Académica — Universidad del Rosario

Aplicación web ASP.NET Core MVC (.NET 10) para gestionar asignaturas, profesores, salones y la
disponibilidad de los profesores de cátedra, y para generar automáticamente la proyección de la
oferta académica (con exportación a Excel).

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (verifica con `dotnet --version`).
- Git.
- Opcional: Python 3, solo para la simulación de escenarios (`herramientas/simulacion`).

No hace falta instalar ninguna base de datos: se usa SQLite y el archivo se crea solo.

## Levantar la aplicación

```powershell
git clone https://github.com/Nikolukas17/CAPSTONE-PROJECT.git
cd CAPSTONE-PROJECT
dotnet run --launch-profile http
```

Abre http://localhost:5281.

En el primer arranque la aplicación:

1. Crea `App_Data/oferta_academica.db` y aplica las migraciones.
2. Carga el catálogo inicial (asignaturas, profesores, salones) desde `Data/ImportacionExcel.json`
   y la planeación de pregrado desde `Data/PlanificacionPregrado.json`.

## Primer uso

1. Entra a **Registro** y crea una cuenta. Solo se aceptan correos `@urosario.edu.co`
   (no se envía correo de verificación, cualquier dirección con ese dominio sirve).
2. Inicia sesión.
3. Revisa los catálogos (Asignaturas, Profesores, Salones). Los profesores de cátedra necesitan
   disponibilidad registrada para poder recibir grupos: se puede marcar en su formulario o enviarles
   el enlace de disponibilidad desde su detalle.
4. En **Oferta Académica → Generar** crea una proyección y expórtala a Excel.

## Correo (opcional)

Sin configuración SMTP, en modo Development los correos (recuperación de contraseña y enlaces de
disponibilidad) **no se envían**: se guardan como `.html` en `App_Data/correos-desarrollo/` y el
enlace se imprime en la consola. Es suficiente para probar todo el flujo.

Para enviar correos reales con Gmail:

1. Copia `appsettings.Local.example.json` como `appsettings.Local.json`.
2. Completa `From`, `Username` y `Password` (contraseña de aplicación de Gmail de 16 letras).

`appsettings.Local.json` está en `.gitignore`: **nunca subas credenciales al repositorio.**
Más detalle en [CONFIGURACION_PRODUCCION.md](CONFIGURACION_PRODUCCION.md).

## Simulación de escenarios (opcional)

Levanta la app con una base de datos nueva y aparte, genera tres escenarios de oferta y deja un
Excel por escenario en `simulacion-salida/<fecha>/salida/`. No toca la base real.

```powershell
.\herramientas\simulacion\simular.ps1                # corre y cierra la app al terminar
.\herramientas\simulacion\simular.ps1 -DejarAbierta  # deja la app abierta en http://127.0.0.1:5898
```

Si PowerShell bloquea el script:
`powershell -ExecutionPolicy Bypass -File .\herramientas\simulacion\simular.ps1`

## Problemas frecuentes

| Problema | Solución |
|---|---|
| `dotnet` no reconoce `net10.0` | Instala el .NET 10 SDK. |
| El puerto 5281 está ocupado | `dotnet run --urls http://localhost:5300` |
| Quiero empezar con datos limpios | Detén la app y borra la carpeta `App_Data/`; se recrea al iniciar. |
| No llega el correo de recuperación | Sin SMTP es normal: busca el `.html` en `App_Data/correos-desarrollo/`. |

## Estructura

- `Controllers/`, `Views/`, `ViewModels/` — MVC.
- `Models/Entities/` — entidades de EF Core.
- `Data/` — `AppDbContext`, semillas y datos iniciales (JSON).
- `Migrations/` — migraciones de EF Core (se aplican solas al iniciar).
- `Services/` — generador de oferta, exportación a Excel, correo.
- `herramientas/simulacion/` — simulación de escenarios.

Documentación adicional: `README_*.md` y `CONFIGURACION_PRODUCCION.md`.
