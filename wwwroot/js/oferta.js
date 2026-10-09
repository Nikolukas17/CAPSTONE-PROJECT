/* Oferta académica: modales de detalle de clase y de franja horaria. */
(function () {
  'use strict';

  var clases = window.OFERTA_CLASES || [];
  var cal = document.getElementById('calendario');
  var modalEl = document.getElementById('modalClase');
  if (!cal || !modalEl) return;

  var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
  var vistaFranja = modalEl.querySelector('[data-vista="franja"]');
  var vistaDetalle = modalEl.querySelector('[data-vista="detalle"]');
  var urlDetalle = cal.getAttribute('data-url-detalle');
  var NOMBRES_DIA = ['lunes', 'martes', 'miércoles', 'jueves', 'viernes'];
  var ABREV_DIA = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie'];
  var franjaActual = null;

  function q(sel, raiz) { return (raiz || modalEl).querySelector(sel); }
  function texto(sel, valor) { q(sel).textContent = valor; }

  function mostrarDetalle(c, desdeFranja) {
    texto('[data-det-nombre]', c.nombre);
    texto('[data-det-codigo]', 'Código ' + c.codigo + ' · Grupo ' + c.grupo);
    texto('[data-det-horario]', c.horario);
    texto('[data-det-salon]', c.salon);
    texto('[data-det-creditos]', String(c.creditos));
    texto('[data-det-cupos]', c.inscritos + ' de ' + c.cupos + ' inscritos');
    texto('[data-det-programa]', c.programa);
    texto('[data-det-profesor]', c.profesor);
    q('[data-det-enlace]').setAttribute('href', urlDetalle + '/' + c.asignaturaId);

    var volver = q('[data-volver]');
    if (desdeFranja && franjaActual) {
      volver.hidden = false;
      texto('[data-volver-texto]', 'Volver a la franja ' + ABREV_DIA[franjaActual.dia] + ' ' + franjaActual.hora + ':00 – ' + (franjaActual.hora + 2) + ':00');
    } else {
      volver.hidden = true;
    }
    vistaFranja.hidden = true;
    vistaDetalle.hidden = false;
  }

  function mostrarFranja(dia, hora) {
    franjaActual = { dia: dia, hora: hora };
    var lista = clases.filter(function (c) { return c.dia === dia && c.hora === hora; });
    texto('#tituloFranja', 'Franja ' + hora + ':00 – ' + (hora + 2) + ':00');
    texto('[data-franja-resumen]', lista.length + ' asignaturas programadas el ' + NOMBRES_DIA[dia] + '.');

    var ul = q('[data-franja-lista]');
    ul.textContent = '';
    lista.forEach(function (c) {
      var li = document.createElement('li');
      var btn = document.createElement('button');
      btn.type = 'button';
      var izq = document.createElement('span');
      var nombre = document.createElement('strong');
      nombre.textContent = c.nombre;
      var sub = document.createElement('small');
      sub.textContent = c.profesor;
      izq.appendChild(nombre);
      izq.appendChild(sub);
      var der = document.createElement('span');
      der.className = 'ur-muted';
      der.textContent = c.salon;
      btn.appendChild(izq);
      btn.appendChild(der);
      btn.addEventListener('click', function () { mostrarDetalle(c, true); });
      li.appendChild(btn);
      ul.appendChild(li);
    });

    vistaDetalle.hidden = true;
    vistaFranja.hidden = false;
    modal.show();
  }

  cal.addEventListener('click', function (e) {
    var principal = e.target.closest('[data-clase]');
    if (principal) {
      franjaActual = null;
      mostrarDetalle(clases[parseInt(principal.getAttribute('data-clase'), 10)], false);
      modal.show();
      return;
    }
    var mas = e.target.closest('[data-franja-dia]');
    if (mas) {
      mostrarFranja(parseInt(mas.getAttribute('data-franja-dia'), 10), parseInt(mas.getAttribute('data-franja-hora'), 10));
    }
  });

  q('[data-volver]').addEventListener('click', function () {
    if (franjaActual) mostrarFranja(franjaActual.dia, franjaActual.hora);
  });
})();
