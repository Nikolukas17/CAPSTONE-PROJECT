// Atajos para llenar la cuadrícula de disponibilidad (franjas de 30 min desde las 7:00; día 0 = lunes).
// En modo bloques de 2 horas cada botón lleva data-f = primera franja del bloque y su celda guarda 4 franjas ocultas.
(function () {
  const TEXTOS = { Disponible: 'Disponible', NoDisponible: 'No disponible' };
  const H = (h) => (h - 7) * 2; // hora -> índice de franja

  // [días, franja inicial, franja final (exclusiva)]
  const PRESETS = {
    jornada: [[0, 1, 2, 3, 4, 5], H(7), H(17)],
    mananas: [[0, 1, 2, 3, 4], H(7), H(13)],
    tardes: [[0, 1, 2, 3, 4], H(13), H(17)],
    noches: [[0, 1, 2, 3, 4], H(17), H(19)],
    sabado: [[5], H(7), H(13)],
  };

  function fijar(slot, estado) {
    slot.dataset.estado = estado;
    slot.textContent = TEXTOS[estado];
    slot.setAttribute('aria-label', slot.dataset.dia + ' ' + slot.dataset.franja + ': ' + TEXTOS[estado]);
    slot.parentElement.querySelectorAll('input[type="hidden"]').forEach((oculto) => { oculto.value = estado; });
  }

  function aplicar(grid, nombre, reemplazar) {
    const slots = grid.querySelectorAll('button.slot[data-f]');
    if (nombre === 'limpiar' || reemplazar) slots.forEach((s) => fijar(s, 'NoDisponible'));
    const preset = PRESETS[nombre];
    if (!preset) return;
    const [dias, desde, hasta] = preset;
    slots.forEach((s) => {
      const f = +s.dataset.f, d = +s.dataset.d;
      if (dias.includes(d) && f >= desde && f < hasta) fijar(s, 'Disponible');
    });
  }

  // Botones: <button data-preset="mananas" data-grid="#id-del-contenedor">
  document.querySelectorAll('[data-preset]').forEach((btn) =>
    btn.addEventListener('click', () => {
      const cont = document.querySelector(btn.dataset.grid) || btn.closest('form');
      const grid = cont && cont.querySelector('[data-grid-editable]');
      if (grid) aplicar(grid, btn.dataset.preset, btn.dataset.reemplazar === 'true');
    }));

  // Páginas sin catalogos.js (p. ej. el enlace público del profesor): alternar al hacer clic.
  document.querySelectorAll('[data-grid-autonomo] button.slot').forEach((slot) =>
    slot.addEventListener('click', () =>
      fijar(slot, slot.dataset.estado === 'Disponible' ? 'NoDisponible' : 'Disponible')));

  window.DisponibilidadAtajos = { aplicar };
})();
