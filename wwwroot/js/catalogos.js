(function () {
  // ---------- Aviso breve (acciones aún sin backend) ----------
  function aviso(texto) {
    let t = document.getElementById('toast');
    if (!t) {
      t = document.createElement('div');
      t.id = 'toast';
      t.className = 'toast';
      t.setAttribute('role', 'status');
      document.body.appendChild(t);
    }
    t.textContent = texto;
    t.classList.add('is-visible');
    clearTimeout(aviso._t);
    aviso._t = setTimeout(() => t.classList.remove('is-visible'), 3200);
  }
  document.querySelectorAll('[data-demo]').forEach((el) =>
    el.addEventListener('click', () => aviso(el.dataset.demo)));

  // ---------- Inactivar asignatura (diálogo) ----------
  const dlg = document.getElementById('modalInactivar');
  if (dlg) {
    const form = document.getElementById('formInactivar');
    const nombre = dlg.querySelector('[data-modal-nombre]');
    document.querySelectorAll('[data-inactivar]').forEach((btn) =>
      btn.addEventListener('click', () => {
        form.action = btn.dataset.action;
        nombre.textContent = btn.dataset.nombre;
        dlg.showModal();
      }));
    dlg.addEventListener('click', (e) => {
      if (e.target === dlg || e.target.closest('[data-cerrar]')) dlg.close();
    });
  }

  // ---------- Formularios de detalle (asignatura y profesor) ----------
  const formDetalle = document.querySelector('.form-asig');
  if (!formDetalle) return;

  // Ver -> Editar (solo asignaturas)
  const campos = formDetalle.querySelector('[data-campos]');
  const btnEditar = formDetalle.querySelector('[data-accion="editar"]');
  if (btnEditar && campos) {
    btnEditar.addEventListener('click', () => {
      campos.disabled = false;
      formDetalle.dataset.modo = 'editar';
    });
  }

  // Contadores (créditos / intensidad)
  formDetalle.querySelectorAll('[data-stepper]').forEach((st) => {
    const input = st.querySelector('input');
    st.querySelectorAll('[data-paso]').forEach((b) =>
      b.addEventListener('click', () => {
        const min = +input.min || 0, max = +input.max || 999;
        const v = (parseInt(input.value, 10) || min) + parseInt(b.dataset.paso, 10);
        input.value = Math.min(max, Math.max(min, v));
        input.dispatchEvent(new Event('input', { bubbles: true }));
      }));
  });

  // Disponibilidad: alternar Disponible <-> No disponible
  const textos = { Disponible: 'Disponible', NoDisponible: 'No disponible' };
  formDetalle.querySelectorAll('[data-grid-editable] button.slot').forEach((slot) =>
    slot.addEventListener('click', () => {
      const nuevo = slot.dataset.estado === 'Disponible' ? 'NoDisponible' : 'Disponible';
      slot.dataset.estado = nuevo;
      slot.textContent = textos[nuevo];
      slot.setAttribute('aria-label', slot.dataset.dia + ' ' + slot.dataset.franja + ': ' + textos[nuevo]);
      const oculto = slot.parentElement.querySelector('input[type="hidden"]');
      if (oculto) oculto.value = nuevo;
    }));

  // Materias del profesor: filtro y contador
  const filtro = formDetalle.querySelector('[data-filtro-materias]');
  const items = formDetalle.querySelectorAll('.materia-item');
  const contador = formDetalle.querySelector('[data-contador-materias]');
  if (filtro) {
    filtro.addEventListener('input', () => {
      const q = filtro.value.trim().toLowerCase();
      items.forEach((li) => { li.hidden = q !== '' && !li.dataset.texto.includes(q); });
    });
  }
  if (contador) {
    const actualizar = () => {
      contador.textContent = formDetalle.querySelectorAll('.materia-item input:checked').length;
    };
    items.forEach((li) => li.querySelector('input').addEventListener('change', actualizar));
    actualizar();
  }
  // El buscador de materias no debe enviar el formulario con Enter
  if (filtro) filtro.addEventListener('keydown', (e) => { if (e.key === 'Enter') e.preventDefault(); });
})();
