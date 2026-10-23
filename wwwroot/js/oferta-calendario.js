(function () {
  const sesiones = JSON.parse(document.getElementById('oferta-data').textContent || '[]');
  const diasCorto = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
  const diasLargo = ['DOMINGO', 'LUNES', 'MARTES', 'MIÉRCOLES', 'JUEVES', 'VIERNES', 'SÁBADO'];
  const meses = ['ENERO', 'FEBRERO', 'MARZO', 'ABRIL', 'MAYO', 'JUNIO', 'JULIO', 'AGOSTO', 'SEPTIEMBRE', 'OCTUBRE', 'NOVIEMBRE', 'DICIEMBRE'];
  const colores = { rojo: '#d92d4b', verde: '#16a34a', azul: '#2563eb', amarillo: '#d97706', rosa: '#db2777', gris: '#667085' };
  const $ = (id) => document.getElementById(id);

  const dlgFranja = $('dlg-franja');
  const dlgDetalle = $('dlg-detalle');
  const duracion = parseInt(dlgFranja.dataset.duracion || '2', 10);

  const horaInicio = (s) => parseInt(s.inicio.slice(0, 2), 10);
  const icono = (clase) => { const i = document.createElement('i'); i.className = 'bi ' + clase; return i; };

  function abrirDetalle(s) {
    $('det-horario').textContent = diasCorto[s.dia] + ' · ' + s.horario;
    $('det-nombre').textContent = s.nombre;
    $('det-prof').textContent = s.profesor;
    $('det-ini').textContent = s.profesor.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0]).join('').toUpperCase();
    $('det-cod').textContent = s.codigo;
    $('det-cupo').textContent = s.cupo + ' cupos';
    $('det-dia').textContent = diasCorto[s.dia] + ', ' + s.horario;
    $('det-salon').textContent = s.salon;
    $('det-grupo').textContent = s.grupo;
    $('det-prog').textContent = s.programa;
    $('det-link').href = (dlgDetalle.dataset.urlDetalle || '#').replace('__ID__', s.asignaturaId);
    dlgDetalle.showModal();
  }

  function abrirFranja(dia, hora, fecha) {
    const [, m, d] = fecha.split('-').map(Number);
    const lista = sesiones.filter((s) => s.dia === dia && horaInicio(s) >= hora && horaInicio(s) < hora + duracion);

    $('fr-fecha').textContent = diasLargo[dia] + ' ' + d + ' DE ' + meses[m - 1];
    $('fr-titulo').textContent = 'Franja ' + hora + ':00 – ' + (hora + duracion) + ':00';
    $('fr-sub').textContent = lista.length + (lista.length === 1 ? ' asignatura programada' : ' asignaturas programadas') + ' en esta franja';

    const ul = $('fr-lista');
    ul.replaceChildren();
    lista.forEach((s) => {
      const li = document.createElement('li');
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'fr-item';
      btn.style.setProperty('--c', colores[s.color] || colores.gris);

      const izq = document.createElement('div');
      const nombre = document.createElement('strong');
      nombre.textContent = s.nombre;
      const meta = document.createElement('small');
      meta.textContent = s.codigo + ' · Grupo ' + s.grupo + ' · ' + s.salon;
      izq.append(nombre, meta);

      const der = document.createElement('div');
      der.className = 'fr-prof';
      const prof = document.createElement('span');
      prof.textContent = s.profesor;
      der.append(icono('bi-person'), prof, icono('bi-chevron-right'));

      btn.append(izq, der);
      btn.addEventListener('click', () => { dlgFranja.close(); abrirDetalle(s); });
      li.appendChild(btn);
      ul.appendChild(li);
    });
    dlgFranja.showModal();
  }

  document.querySelectorAll('.franja').forEach((el) =>
    el.addEventListener('click', () => abrirFranja(+el.dataset.dia, +el.dataset.hora, el.dataset.fecha)));

  [dlgFranja, dlgDetalle].forEach((dlg) => {
    dlg.addEventListener('click', (e) => {
      if (e.target === dlg || e.target.closest('[data-close]')) dlg.close();
    });
  });
})();
