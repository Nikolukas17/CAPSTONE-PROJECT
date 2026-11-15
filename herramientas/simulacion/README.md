# Simulación de la oferta académica: paso a paso

La simulación genera automáticamente **tres proyecciones de oferta** (escenarios A, B y C) para comparar
qué pasa con y sin la disponibilidad de los profesores de cátedra, y qué pasa si los grupos se ajustan a
la capacidad de los salones.

Corre sobre una **base de datos nueva y aparte**: tu base real (`App_Data/oferta_academica.db`) no se usa
ni se modifica. Puedes correrla cuantas veces quieras.

---

## 1. Requisitos

| Herramienta | Para qué | Cómo verificar |
|---|---|---|
| .NET 10 SDK | Compilar y levantar la app | `dotnet --version` → `10.x` |
| Python 3 | Ejecutar los escenarios (solo librería estándar, no hay que instalar paquetes) | `python --version` → `3.x` |
| PowerShell | Ejecutar el script (viene con Windows) | — |

Si `python` no se reconoce, instálalo desde https://www.python.org/downloads/ marcando
**"Add python.exe to PATH"** y abre una terminal nueva.

## 2. Ejecutar

Abre PowerShell **en la carpeta raíz del proyecto** (donde está `OfertaAcademica.Web.csproj`) y elige:

```powershell
# Corre la simulación y cierra la app al terminar (solo deja los resultados en archivos)
.\herramientas\simulacion\simular.ps1

# Corre la simulación y deja la app abierta para revisarla en el navegador (recomendado la primera vez)
.\herramientas\simulacion\simular.ps1 -DejarAbierta
```

Si aparece *"la ejecución de scripts está deshabilitada en este sistema"*:

```powershell
powershell -ExecutionPolicy Bypass -File .\herramientas\simulacion\simular.ps1 -DejarAbierta
```

Tarda entre 30 segundos y 1 minuto. Verás cuatro pasos:

```
1/4 Compilando...
2/4 Iniciando la app con una base nueva en ...\simulacion-salida\<fecha>\root ...
3/4 Generando los escenarios A, B y C (tarda unos segundos)...
    (resumen de cada escenario)
4/4 App cerrada.            ← o "La app sigue abierta en http://127.0.0.1:5898" con -DejarAbierta
```

> Los tildes pueden verse como `�` en la consola. Es solo la codificación de la terminal; los archivos
> generados están bien.

## 3. Qué hace por dentro

1. Compila el proyecto en `simulacion-salida/<fecha>/app/` (no choca con un `dotnet run` abierto).
2. Levanta la app con una base vacía en `simulacion-salida/<fecha>/root/`. Al arrancar se cargan el
   catálogo (asignaturas, profesores, salones) y la planeación de pregrado, igual que en una instalación nueva.
3. Crea la cuenta `simulacion@urosario.edu.co` e inicia sesión.
4. Genera los tres escenarios por la misma pantalla **Oferta Académica → Generar** que usaría la coordinadora,
   y descarga el Excel de cada uno.
5. Los cambios temporales de los escenarios B y C se revierten después de generar cada uno.

## 4. Los tres escenarios

| Escenario | Qué simula | Pregunta que responde |
|---|---|---|
| **A. Planta + cátedra simulada** | Profesores de planta (jornada fija 7:00–17:00) + 20 profesores de cátedra con disponibilidad **de ejemplo** (mañana, tarde, noche, jornada amplia). | ¿Cómo quedaría la oferta si la cátedra entrega su disponibilidad? |
| **B. Solo planta** | Igual que A, pero la cátedra **sin** disponibilidad registrada. | ¿Qué pasa hoy, mientras la coordinadora no registre la disponibilidad de cátedra? |
| **C. Grupos ajustados a capacidad** | Igual que A, pero las asignaturas cuyo cupo no cabe en la regla de capacidad o en el salón exigido se dividen en más grupos (misma demanda total). | ¿Cuánto mejora si la planeación respeta la capacidad de los salones? |

> Los 20 profesores de cátedra y sus disponibilidades son **datos simulados**. Sirven para probar el
> generador, no son asignaciones reales.

## 5. Revisar en el navegador (con `-DejarAbierta`)

1. Abre **http://127.0.0.1:5898**.
2. Inicia sesión con:
   - Usuario: `simulacion@urosario.edu.co`
   - Clave: `Clave-Segura-2026`
3. Ve a **Oferta Académica**. Están las tres proyecciones:
   `A_Base_catedra_simulada`, `B_Solo_planta`, `C_Planeacion_ajustada`.
4. En cada una puedes ver el horario por semana, los grupos que no se pudieron programar con su motivo, y
   **Exportar a Excel**.
5. También puedes recorrer Profesores (ver la disponibilidad de cátedra simulada), Salones y Asignaturas.
   Todo lo que cambies aquí afecta solo a la base de la simulación.

## 6. Resultados e interpretación

Los archivos quedan en `simulacion-salida/<fecha>/salida/`:

- `A_Base_catedra_simulada.xlsx`, `B_Solo_planta.xlsx`, `C_Planeacion_ajustada.xlsx`: la oferta de cada escenario.
- `resultados.json`: el resumen completo (fallas con su detalle, carga por profesor, uso de salones, sesiones por día).

### Valores de referencia

Con los datos iniciales del repositorio, la simulación debe dar exactamente esto. **Si obtienes los mismos
números, tu instalación quedó bien.**

| Escenario | Grupos programados | Profesores sin curso | Principales motivos de grupos sin programar |
|---|---|---|---|
| A | **133 / 183** (73 %) | 1 | Sin salón con capacidad (17), profesores sin disponibilidad o al tope (13), choques de horario (12), cupo excede la regla de capacidad (8) |
| B | **59 / 183** (32 %) | 21 | Profesores sin disponibilidad o al tope (109), sin salón con capacidad (10), cupo excede la regla (5) |
| C | **141 / 207** (68 %) | 0 | Choques de horario (43), profesores sin disponibilidad o al tope (23) |

Los tres quedan en estado **Parcial**: es lo esperado, no un error. Significa que el generador programó lo
que pudo respetando todas las reglas y reporta el resto con su motivo.

### Qué significa cada motivo

| Motivo | Qué pasó | Cómo se resolvería en la realidad |
|---|---|---|
| Profesores sin disponibilidad o al tope de carga | Ningún profesor asociado a la asignatura tiene franjas libres o todos llegaron a su máximo de cursos. | Registrar la disponibilidad de cátedra (enlace de disponibilidad) o asociar más profesores. |
| Sin salón con capacidad suficiente | Ningún salón permitido para la asignatura tiene capacidad para el cupo. | Dividir en más grupos o habilitar otro salón. |
| Cupo excede la regla de capacidad | El cupo del grupo supera el máximo de su tipo (p. ej. teórico-práctica 25). | Ajustar el número de grupos en la planeación. |
| Choque de horarios | Hay profesor y salón, pero no coinciden en ninguna franja libre. | Ampliar la disponibilidad o los salones permitidos. |
| Sin profesor asociado | La asignatura no tiene profesores activos. | Asociar profesores en el catálogo. |

### Conclusiones principales

- **La disponibilidad de cátedra es decisiva**: sin ella (B) se programa menos de la mitad que en A y 21
  profesores de planta quedan sin curso. Por eso existe el módulo del enlace de disponibilidad.
- **Ajustar los grupos a la capacidad (C)** elimina las fallas de capacidad y reparte mejor los salones,
  pero al haber más grupos aparecen más choques de horario.
- En A y C aparece **1 sesión el sábado**; según las reglas, el sábado es solo para Capstone.

## 7. Cerrar y limpiar

- Con `-DejarAbierta`, la consola muestra el comando para cerrar la app, por ejemplo:
  ```powershell
  Stop-Process -Id 15512
  ```
  Si lo perdiste, cierra el proceso que usa el puerto:
  ```powershell
  Stop-Process -Id (Get-NetTCPConnection -LocalPort 5898 -State Listen).OwningProcess
  ```
- Cada corrida crea una carpeta nueva en `simulacion-salida/`. Puedes borrar las que ya no necesites
  (la carpeta está en `.gitignore`, no se sube al repositorio).

## 8. Problemas frecuentes

| Problema | Solución |
|---|---|
| *"El puerto 5898 está ocupado"* | Hay una simulación anterior abierta: ciérrala (ver paso 7) o usa otro puerto: `.\herramientas\simulacion\simular.ps1 -DejarAbierta -Puerto 5899`. |
| *"python no se reconoce..."* | Instala Python 3 con "Add to PATH" y abre una terminal nueva. |
| *"La compilación falló"* | Ejecuta `dotnet build` en la raíz del proyecto para ver el error. |
| *"La app no arrancó"* | Revisa `simulacion-salida/<fecha>/app-errores.log`. |
| Los números no coinciden con los de referencia | Puede que los datos iniciales (`Data/*.json`) o el generador hayan cambiado; compara `resultados.json` con el equipo. |

## Uso avanzado

El script de Python también se puede ejecutar solo, con la app ya corriendo sobre la carpeta de la simulación:

```powershell
python herramientas\simulacion\simular_oferta.py <carpeta_simulacion> [puerto]
```
