(function () {
  function enhance(row) {
    let startX = null;
    row.addEventListener('touchstart', (e) => { startX = e.changedTouches[0].clientX; }, { passive: true });
    row.addEventListener('touchend', (e) => {
      if (startX == null) return;
      const dx = e.changedTouches[0].clientX - startX;
      if (Math.abs(dx) < 60) return;
      row.classList.add(dx < 0 ? 'dd-swipe-left' : 'dd-swipe-right');
      const edit = row.querySelector('[data-swipe-edit]');
      const del = row.querySelector('[data-swipe-delete]');
      if (dx < 0 && edit) edit.click();
      if (dx > 0 && del) {
        if (confirm('Delete this transaction?')) del.click();
      }
      setTimeout(() => row.classList.remove('dd-swipe-left', 'dd-swipe-right'), 400);
      startX = null;
    }, { passive: true });
  }
  document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-swipe-tx]').forEach(enhance);
  });
})();
