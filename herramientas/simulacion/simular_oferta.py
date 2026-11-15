"""Simulación de la oferta académica en tres escenarios.

Se ejecuta con simular.ps1, que levanta la app con una base de datos NUEVA y aparte (no toca la base real).
Uso directo:  python simular_oferta.py <carpeta_simulacion> [puerto]   (la app debe estar corriendo en 127.0.0.1:<puerto>, 5898 por defecto,
con --contentRoot <carpeta_simulacion>/root).

Escenarios:
  A. Planta (jornada fija 7-17) + 20 profesores de cátedra con disponibilidad SIMULADA.
  B. Solo planta: la cátedra sin disponibilidad (situación real mientras la coordinadora no la registre).
  C. Como A, pero con más grupos donde el cupo no cabe en la regla de capacidad o en el salón exigido.
Los cambios de B y C se revierten después de generar cada proyección.
Salida: un Excel por escenario y resultados.json en <carpeta_simulacion>/salida.
"""
import http.cookiejar, json, math, re, sqlite3, sys, urllib.parse, urllib.request, collections, os

SIM = sys.argv[1]
BASE = f"http://127.0.0.1:{sys.argv[2] if len(sys.argv) > 2 else 5898}"
DB = os.path.join(SIM, 'root', 'App_Data', 'oferta_academica.db')
OUT = os.path.join(SIM, 'salida')
jar = http.cookiejar.CookieJar()

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k): return None
op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar), NoRedirect)

def req(path, data=None, raw=False):
    body = urllib.parse.urlencode(data, doseq=True).encode() if data is not None else None
    try:
        r = op.open(BASE + path, body)
        b = r.read()
        return r.status, r.headers.get('Location'), b if raw else b.decode('utf-8')
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get('Location'), e.read().decode('utf-8', 'replace')

token = lambda p: re.search(r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', p).group(1)

# --- Cuenta de simulación
_, _, p = req('/Account/Registro')
req('/Account/Registro', {'__RequestVerificationToken': token(p), 'NombreCompleto': 'Simulacion', 'Cargo': 'Coordinadora',
    'Correo': 'simulacion@urosario.edu.co', 'Contrasena': 'Clave-Segura-2026', 'ConfirmarContrasena': 'Clave-Segura-2026', 'AceptaTerminos': 'true'})
_, _, p = req('/Account/Login')
s, _, _ = req('/Account/Login', {'__RequestVerificationToken': token(p), 'Correo': 'simulacion@urosario.edu.co', 'Contrasena': 'Clave-Segura-2026'})
assert s == 302, 'login falló'

con = sqlite3.connect(DB)
q = lambda sql, *a: con.execute(sql, a).fetchall()

def generar(nombre):
    _, _, p = req('/OfertaAcademica/Generar')
    form = {'__RequestVerificationToken': token(p), 'NombreProyeccion': nombre}
    for tag in re.findall(r'<input[^>]*name="Asignaturas\[\d+\]\.[A-Za-z]+"[^>]*>', p):
        name = re.search(r'name="([^"]+)"', tag).group(1)
        val = re.search(r'value="([^"]*)"', tag)
        if 'type="checkbox"' in tag: form[name] = 'true'
        elif name not in form: form[name] = val.group(1) if val else ''
    s, _, _ = req('/OfertaAcademica/Generar', form)
    assert s == 302, f'generar {nombre} -> {s}'
    pid = q("select max(Id) from ProyeccionesOferta")[0][0]
    s, _, x = req(f'/OfertaAcademica/ExportarExcel/{pid}', raw=True)
    if s == 200:
        open(os.path.join(OUT, f'{nombre}.xlsx'), 'wb').write(x)
    return pid

def clasificar(linea):
    if 'no hay salones compatibles' in linea: return 'Sin salón con capacidad suficiente'
    if 'incumple la regla de capacidad' in linea: return 'Cupo excede la regla de capacidad'
    if 'no tienen disponibilidad registrada o ya alcanzaron' in linea: return 'Profesores sin disponibilidad o al tope de carga'
    if 'no tiene profesores activos asociados' in linea: return 'Sin profesor asociado'
    if 'no se encontró una combinación' in linea: return 'Choque de horarios (profesor/salón/franja)'
    if 'mínimo configurado' in linea: return None  # advertencia de carga mínima, no es grupo fallido
    if linea.startswith('Advertencia'): return None
    return 'Otro'

def resumir(pid, nombre):
    est, sol, prog, msg = q("select Estado, TotalGruposSolicitados, TotalGruposProgramados, Mensaje from ProyeccionesOferta where Id=?", pid)[0]
    lineas = [l for l in msg.splitlines() if l.strip()]
    fallas = collections.Counter(); detalle = collections.defaultdict(list)
    for l in lineas:
        c = clasificar(l)
        if c: fallas[c] += 1; detalle[c].append(l)
    por_tipo = q("""select p.Tipo, count(*), count(distinct p.Id) from GruposOferta g join Profesores p on p.Id=g.ProfesorId
                    where g.ProyeccionOfertaId=? group by 1""", pid)
    profes_total = dict(q("select Tipo, count(*) from Profesores where Activo=1 group by 1"))
    salones = q("""select s.Nombre, s.Capacidad, count(distinct g.Id), coalesce(sum((se.FinMinutos-se.InicioMinutos)/60.0),0)
                   from Salones s left join GruposOferta g on g.SalonCodigo=s.Codigo and g.ProyeccionOfertaId=?
                   left join SesionesOferta se on se.GrupoOfertaId=g.Id where s.Activo=1 group by s.Codigo order by 4 desc""", pid)
    dias = dict(q("""select se.Dia, count(*) from SesionesOferta se join GruposOferta g on g.Id=se.GrupoOfertaId
                     where g.ProyeccionOfertaId=? group by 1""", pid))
    aula_general = q("select count(*) from GruposOferta where ProyeccionOfertaId=? and SalonCodigo is null", pid)[0][0]
    carga = q("""select p.Nombre, p.Tipo, count(*) cursos, p.CursosMaximos from GruposOferta g join Profesores p on p.Id=g.ProfesorId
                 where g.ProyeccionOfertaId=? group by p.Id order by cursos desc""", pid)
    sin_curso = q("""select Nombre, Tipo from Profesores where Activo=1 and Id not in
                     (select ProfesorId from GruposOferta where ProyeccionOfertaId=?)""", pid)
    asign_sin = q("""select count(*) from (select a.Codigo from Asignaturas a where a.Activa=1 group by a.Codigo)""")[0][0]
    return dict(nombre=nombre, id=pid, estado=est, solicitados=sol, programados=prog, fallas=dict(fallas.most_common()),
                detalle={k: v for k, v in detalle.items()}, por_tipo=por_tipo, profes_total=profes_total, salones=salones,
                dias=dias, aula_general=aula_general, al_tope=sum(1 for c in carga if c[2] >= c[3]),
                sin_curso=sin_curso, carga=carga[:10])

resultados = []

# A. Base: planta jornada fija 7–17 + 20 profesores de cátedra con disponibilidad simulada
resultados.append(resumir(generar('A_Base_catedra_simulada'), 'A. Planta + cátedra simulada'))

# B. Solo planta: la cátedra aún no ha entregado disponibilidad (situación real hoy)
respaldo = q("select Id, Disponibilidad from Profesores where Tipo like 'C%tedra%'")
vacia = json.dumps([2] * 144)
con.executemany("update Profesores set Disponibilidad=? where Id=?", [(vacia, i) for i, _ in respaldo]); con.commit()
resultados.append(resumir(generar('B_Solo_planta'), 'B. Solo planta (cátedra sin disponibilidad)'))
con.executemany("update Profesores set Disponibilidad=? where Id=?", [(d, i) for i, d in respaldo]); con.commit()

# C. Base + planeación ajustada: más grupos donde el cupo no cabe en la regla de capacidad o en los salones exigidos
reglas = {"Teórica": 40, "Teórico-práctica": 25, "Laboratorio": 40, "Física teórica": 45, "Física laboratorio": 15}
salon_cap = {n.replace(' ', '').upper(): c for n, c in q("select Nombre, Capacidad from Salones where Activo=1")}
salon_cap.update({c.replace(' ', '').upper(): cap for c, cap in q("select Codigo, Capacidad from Salones where Activo=1")})
import unicodedata
norm = lambda s: ''.join(ch for ch in unicodedata.normalize('NFD', s) if unicodedata.category(ch) != 'Mn').strip().upper()
asigs = {norm(n): (t, csv) for n, t, csv in q("select Nombre, TipoAsignatura, SalonesPermitidosCsv from Asignaturas where Activa=1")}
planes = q("select Id, NombreAsignatura, DatosJson from PlanificacionPregrado")
respaldo_plan = [(d, i) for i, _, d in planes]
ajustes = []
for pid_, nombre, datos in planes:
    a = asigs.get(norm(nombre))
    if not a: continue
    d = json.loads(datos); est = d.get('num_estimados') or 0; g = d.get('num_grupos') or 0
    if est <= 0 or g <= 0: continue
    limite = reglas.get(a[0], 500)
    caps = [salon_cap.get(x.replace(' ', '').upper()) for x in a[1].split(',') if x.strip()]
    caps = [c for c in caps if c]
    if caps: limite = min(limite, max(caps))
    nuevos = max(g, math.ceil(est / limite))
    if a[0] == 'Laboratorio':  # mínimo 20 por grupo
        nuevos = min(nuevos, max(1, math.floor(est / 20)))
    nuevos = min(nuevos, 30)
    if nuevos != g:
        ajustes.append((nombre, g, nuevos, math.ceil(est / g), math.ceil(est / nuevos), limite))
        d['num_grupos'] = nuevos
        con.execute("update PlanificacionPregrado set DatosJson=? where Id=?", (json.dumps(d, ensure_ascii=False), pid_))
# Asignaturas sin fila de planeación: heredan el cupo por defecto del tipo; si el salón exigido es más pequeño,
# se simula dividirlas en subgrupos de laboratorio (misma demanda total, más grupos).
defecto = {"Teórico-práctica": 25, "Laboratorio": 20, "Física laboratorio": 15, "Física teórica": 40}
con_plan = {norm(n) for _, n, d in planes if (json.loads(d).get('num_estimados') or 0) > 0}
insertadas = []
for nombre, tipo, csv, grupos in q("""select Nombre, TipoAsignatura, SalonesPermitidosCsv, min(CantidadGrupos) from Asignaturas
                                      where Activa=1 and SalonesPermitidosCsv<>'' group by Codigo"""):
    if norm(nombre) in con_plan: continue
    caps = [salon_cap.get(x.replace(' ', '').upper()) for x in csv.split(',') if x.strip()]
    caps = [c for c in caps if c]
    cupo = defecto.get(tipo, 30); g = max(1, grupos or 1)
    if not caps or max(caps) >= cupo: continue
    est = cupo * g; nuevos = math.ceil(est / max(caps))
    ajustes.append((nombre, g, nuevos, cupo, math.ceil(est / nuevos), max(caps)))
    existente = [i for i, n, _ in planes if norm(n) == norm(nombre)]
    if existente:  # fila con 0 estimados: se completa con la demanda por defecto
        con.execute("update PlanificacionPregrado set DatosJson=? where Id=?",
                    (json.dumps({"num_estimados": est, "num_grupos": nuevos}), existente[0]))
    else:
        cur = con.execute("insert into PlanificacionPregrado(NombreAsignatura, DatosJson) values (?,?)",
                          (nombre, json.dumps({"num_estimados": est, "num_grupos": nuevos})))
        insertadas.append(cur.lastrowid)
    con_plan.add(norm(nombre))
con.commit()
r = resumir(generar('C_Planeacion_ajustada'), 'C. Planta + cátedra simulada, grupos ajustados a capacidad')
r['ajustes'] = ajustes
resultados.append(r)
con.executemany("update PlanificacionPregrado set DatosJson=? where Id=?", respaldo_plan)
con.executemany("delete from PlanificacionPregrado where Id=?", [(i,) for i in insertadas]); con.commit()

json.dump(resultados, open(os.path.join(OUT, 'resultados.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
for r in resultados:
    print(f"\n=== {r['nombre']} (proyección {r['id']}): {r['programados']}/{r['solicitados']} · {r['estado']}")
    print('  Fallas:', r['fallas'])
    print('  Por tipo de profesor (tipo, grupos, profesores usados):', r['por_tipo'])
    print('  Aula general:', r['aula_general'], '· Profesores al tope:', r['al_tope'], '· Sin curso:', len(r['sin_curso']))
    print('  Sesiones por día:', r['dias'])
    print('  Salones (nombre, cap, grupos, horas):', r['salones'])
    if 'ajustes' in r:
        print('  Ajustes de planeación (asignatura: grupos antes -> después, cupo antes -> después):')
        for nombre, g0, g1, c0, c1, _ in r['ajustes']:
            print(f'    {nombre}: {g0} -> {g1} grupos, cupo {c0} -> {c1}')
print()
print('Archivos en', OUT)
