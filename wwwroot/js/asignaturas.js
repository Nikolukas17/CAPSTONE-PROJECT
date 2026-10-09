/* Asignaturas: listado (modal de inactivar) y detalle (edición, contadores, disponibilidad). */
(function () {
  'use strict';

  // ---------- Listado: modal "Inactivar" ----------
  var modalInactivar = document.getElementById('modalInactivar');
  if (modalInactivar) {
    modalInactivar.addEventListener('show.bs.modal', function (e) {
      var boton = e.relatedTarget;
      if (!boton) return;
      modalInactivar.querySelector('[data-modal-nombre]').textContent = boton.getAttribute('data-nombre') || 'esta asignatura';
      modalInactivar.querySelector('form').setAttribute('action', boton.getAttribute('data-action'));
    });
  }

  // ---------- Detalle ----------
  var form = document.getElementById('formAsignatura');
  if (!form) return;

  var campos = form.querySelector('[data-campos]');

  function entrarEnEdicion() {
    form.setAttribute('data-modo', 'editar');
    campos.disabled = false;
    var primero = form.querySelector('input:not([type=hidden]), select');
    if (primero) primero.focus();
  }

  var botonEditar = form.querySelector('[data-accion="editar"]');
  if (botonEditar) botonEditar.addEventListener('click', entrarEnEdicion);

  // Contadores +/-
  form.querySelectorAll('[data-stepper]').forEach(function (stepper) {
    var input = stepper.querySelector('input');
    stepper.querySelectorAll('[data-paso]').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var min = parseInt(input.min || '0', 10);
        var max = parseInt(input.max || '99', 10);
        var actual = parseInt(input.value || String(min), 10);
        var nuevo = Math.min(max, Math.max(min, actual + parseInt(btn.getAttribute('data-paso'), 10)));
        input.value = nuevo;
        input.dispatchEvent(new Event('input', { bubbles: true }));
        if (window.jQuery && jQuery.validator) jQuery(input).valid();
      });
    });
  });

  // Rejilla de disponibilidad: alterna Disponible <-> No disponible (Ocupado es solo lectura)
  var etiquetas = { Disponible: 'Disponible', NoDisponible: 'No disponible' };
  form.querySelectorAll('button.ur-slot').forEach(function (celda) {
    celda.addEventListener('click', function () {
      var nuevo = celda.getAttribute('data-estado') === 'Disponible' ? 'NoDisponible' : 'Disponible';
      celda.setAttribute('data-estado', nuevo);
      celda.textContent = etiquetas[nuevo];
      celda.setAttribute('aria-label', celda.getAttribute('data-dia') + ' ' + celda.getAttribute('data-franja') + ': ' + etiquetas[nuevo]);
      var oculto = celda.parentElement.querySelector('input[type=hidden]');
      if (oculto) oculto.value = nuevo;
    });
  });

  // Si el servidor devolvió errores de validación, ya llega en modo edición; llevamos el foco al primero.
  var primerError = form.querySelector('.input-validation-error');
  if (primerError) primerError.focus();
})();
