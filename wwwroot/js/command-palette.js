(function () {
  const commands = [
    { name: 'Dashboard', url: '/Dashboard' },
    { name: 'Import / Export', url: '/ImportExport' },
    { name: 'Bank sync', url: '/BankSync' },
    { name: 'Cashflow forecast', url: '/Forecast' },
    { name: 'Financial calendar', url: '/Calendar' },
    { name: 'Report builder', url: '/ReportBuilder' },
    { name: 'Goal analytics', url: '/GoalAnalytics' },
    { name: 'Net worth projections', url: '/NetWorthProjections' },
    { name: 'Team budgets', url: '/TeamBudgets' },
    { name: 'Business expenses', url: '/BusinessExpenses' },
    { name: 'Currency rates', url: '/Currency' },
    { name: 'Household', url: '/Household' },
    { name: 'Settings / Security (2FA)', url: '/Settings/Security' },
    { name: 'Billing', url: '/Billing' }
  ];

  function ensureUi() {
    if (document.getElementById('dd-cmd-palette')) return;
    const wrap = document.createElement('div');
    wrap.id = 'dd-cmd-palette';
    wrap.className = 'dd-cmd-palette d-none';
    wrap.innerHTML = '<div class="dd-cmd-dialog card shadow"><input id="dd-cmd-input" class="form-control" placeholder="Type a command…" autocomplete="off" /><ul id="dd-cmd-list" class="list-group list-group-flush"></ul><div class="small text-muted p-2">Ctrl/Cmd+K</div></div>';
    document.body.appendChild(wrap);
    const input = wrap.querySelector('#dd-cmd-input');
    const list = wrap.querySelector('#dd-cmd-list');
    function render(q) {
      const qq = (q || '').toLowerCase();
      list.innerHTML = '';
      commands.filter(c => !qq || c.name.toLowerCase().includes(qq)).slice(0, 12).forEach(c => {
        const li = document.createElement('button');
        li.type = 'button';
        li.className = 'list-group-item list-group-item-action';
        li.textContent = c.name;
        li.onclick = () => { window.location.href = c.url; };
        list.appendChild(li);
      });
    }
    input.addEventListener('input', () => render(input.value));
    wrap.addEventListener('click', (e) => { if (e.target === wrap) close(); });
    window.__ddOpenCmd = function () { wrap.classList.remove('d-none'); input.value = ''; render(''); setTimeout(() => input.focus(), 0); };
    window.__ddCloseCmd = function close() { wrap.classList.add('d-none'); };
    function close() { wrap.classList.add('d-none'); }
  }

  document.addEventListener('keydown', (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
      e.preventDefault();
      ensureUi();
      window.__ddOpenCmd();
    }
    if (e.key === 'Escape') window.__ddCloseCmd && window.__ddCloseCmd();
  });
  document.addEventListener('DOMContentLoaded', ensureUi);
})();
