/* Comportamientos globales: menú móvil, avisos (toasts), mostrar contraseña y botones "demo". */
(function () {
  'use strict';

  // Menú móvil
  var toggle = document.querySelector('.ur-nav__toggle');
  var menu = document.getElementById('menuPrincipal');
  if (toggle && menu) {
    toggle.addEventListener('click', function () {
      var abierto = menu.classList.toggle('is-open');
      toggle.setAttribute('aria-expanded', abierto ? 'true' : 'false');
    });
  }

  // Avisos: window.urToast('mensaje', 'exito' | 'error' | 'info')
  var contenedor = document.getElementById('urToasts');
  var iconos = { exito: 'i-check', error: 'i-alert', info: 'i-alert' };

  function cerrar(toast) { if (toast && toast.parentNode) toast.parentNode.removeChild(toast); }

  window.urToast = function (mensaje, tipo) {
    if (!contenedor) return;
    tipo = tipo || 'exito';
    var t = document.createElement('div');
    t.className = 'ur-toast' + (tipo === 'exito' ? '' : ' ur-toast--' + tipo);
    t.setAttribute('role', tipo === 'error' ? 'alert' : 'status');
    t.innerHTML = '<svg class="ur-icon"><use href="#' + iconos[tipo] + '"></use></svg><p></p>' +
                  '<button type="button" aria-label="Cerrar aviso" data-cerrar-toast>&times;</button>';
    t.querySelector('p').textContent = mensaje;
    contenedor.appendChild(t);
    setTimeout(function () { cerrar(t); }, 6000);
  };

  // Cierre manual y autocierre de los avisos que llegan desde el servidor (TempData)
  document.addEventListener('click', function (e) {
    var cierre = e.target.closest('[data-cerrar-toast]');
    if (cierre) cerrar(cierre.closest('.ur-toast'));

    // Botones/enlaces que aún no tienen backend: data-demo="mensaje"
    var demo = e.target.closest('[data-demo]');
    if (demo) {
      e.preventDefault();
      window.urToast(demo.getAttribute('data-demo'), 'info');
    }

    // Mostrar / ocultar contraseña
    var ojo = e.target.closest('[data-ver-clave]');
    if (ojo) {
      var input = ojo.parentElement.querySelector('input');
      var oculto = input.type === 'password';
      input.type = oculto ? 'text' : 'password';
      ojo.setAttribute('aria-label', oculto ? 'Ocultar contraseña' : 'Mostrar contraseña');
      ojo.querySelector('use').setAttribute('href', oculto ? '#i-eye-off' : '#i-eye');
    }
  });

  if (contenedor) {
    contenedor.querySelectorAll('.ur-toast').forEach(function (t) { setTimeout(function () { cerrar(t); }, 6000); });
  }
})();
