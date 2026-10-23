// Modal nativo <dialog>: [data-dialog-open="id"] lo abre; [data-dialog-close] lo cierra.
document.addEventListener('click', (e) => {
  const open = e.target.closest('[data-dialog-open]');
  if (open) { document.getElementById(open.dataset.dialogOpen)?.showModal(); return; }
  const close = e.target.closest('[data-dialog-close]');
  if (close) { close.closest('dialog')?.close(); return; }
  if (e.target instanceof HTMLDialogElement) e.target.close(); // clic en el fondo
});
