# Persistencia, cuentas y correo de recuperación

## Base de datos

La aplicación usa SQLite y guarda los datos en `App_Data/oferta_academica.db`, dentro de la carpeta del proyecto. La carpeta se crea al iniciar y las migraciones se aplican automáticamente. El archivo no se elimina al cerrar la aplicación. Para respaldarlo, detén la aplicación y copia ese archivo a una ubicación segura. En despliegues, monta `App_Data` como volumen persistente; no lo ubiques en un almacenamiento efímero.

Si existe el archivo histórico `oferta_academica.db` en la raíz del proyecto y todavía no existe `App_Data/oferta_academica.db`, el arranque lo copia antes de migrar.

## Registro e inicio de sesión

Las cuentas se almacenan en `UsuariosAplicacion`. Las contraseñas se guardan como hashes generados por `PasswordHasher<T>`, nunca en texto plano. Se admiten únicamente direcciones terminadas exactamente en `@urosario.edu.co`. Cada perfil se obtiene de la cuenta autenticada.

## Recuperación de contraseña

La recuperación crea un token aleatorio de un solo uso, almacena únicamente su hash y vence en 30 minutos. Para enviar el enlace se requiere configurar un servidor SMTP. No se incluyeron credenciales ficticias ni se puede enviar correo real sin esos datos.

Configura estas variables de entorno antes de ejecutar:

- `Email__SmtpHost`: servidor SMTP.
- `Email__Port`: puerto (habitualmente 587).
- `Email__EnableSsl`: `true` para STARTTLS/TLS según el proveedor.
- `Email__From`: dirección remitente autorizada.
- `Email__Username`: usuario SMTP, si aplica.
- `Email__Password`: contraseña o token SMTP, si aplica.

### Opción recomendada: Gmail con contraseña de aplicación

1. Usa una cuenta Gmail del equipo con verificación en dos pasos activada.
2. En https://myaccount.google.com/apppasswords crea una "contraseña de aplicación" (16 letras).
3. Copia `appsettings.Local.example.json` como `appsettings.Local.json` (está en `.gitignore`) y completa `From`, `Username` y `Password`. La aplicación lo lee automáticamente al iniciar.

`Email__FromName` (opcional) define el nombre visible del remitente.

### Modo desarrollo sin SMTP

Si se ejecuta en `Development` y no hay SMTP configurado, los correos (recuperación de contraseña y enlaces de disponibilidad) se guardan como `.html` en `App_Data/correos-desarrollo/` y el enlace se escribe en la consola. En cualquier otro entorno, sin SMTP, el envío falla con un error visible: nunca se simula un envío.

No guardes la contraseña SMTP en el repositorio ni en `appsettings.json`. Si la institución no facilita SMTP, usa un proveedor transaccional que autorice el dominio institucional y configura sus credenciales como variables de entorno.

## Datos de planeación

Se importa únicamente la hoja `Pregrado` de `Modelo Planificación Oferta.xlsx`. Se conservan los campos originales de las 91 filas con asignatura, horas semanales, cuatro pares de frecuencia/duración de sesión, necesidades de espacio, monitoría, demanda, estimados y número de grupos, en `PlanificacionPregrado.DatosJson`. La hoja Posgrado no se importa.

Los 20 perfiles de cátedra se identifican explícitamente como simulados y sus disponibilidades son ejemplos de mañana, tarde, noche y jornada amplia. Deben validarse con la coordinación antes de usarlos como asignaciones reales.
