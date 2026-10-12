const CACHE = 'doughdoodle-v15';
const PRECACHE = [
  '/css/site.css',
  '/css/themes.css',
  '/js/site.js',
  '/js/theme-manager.js',
  '/js/command-palette.js',
  '/js/swipe-transactions.js',
  '/js/performance-utils.js',
  '/images/branding/logo-mark.svg',
  '/images/branding/logo-mark.png',
  '/images/branding/logo-wordmark.svg',
  '/images/branding/empty-envelopes.svg',
  '/images/branding/hero-dough.svg',
  '/images/branding/favicon.svg',
  '/offline.html',
  '/manifest.webmanifest',
  '/lib/bootstrap-icons/font/bootstrap-icons.min.css',
  '/lib/bootstrap-icons/font/fonts/bootstrap-icons.woff2'
];

self.addEventListener('install', (event) => {
  event.waitUntil(caches.open(CACHE).then((c) => c.addAll(PRECACHE)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', (event) => {
  // Delete ALL old doughdoodle-* caches aggressively (keep only current CACHE)
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.filter((k) => k.startsWith('doughdoodle-') && k !== CACHE).map((k) => caches.delete(k)))
    ).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (event) => {
  const req = event.request;
  if (req.method !== 'GET') return;
  const url = new URL(req.url);

  // Documents: network-only; never put HTML navigations into Cache Storage.
  // Offline fallback to /offline.html only when fetch fails.
  if (req.mode === 'navigate') {
    event.respondWith(fetch(req).catch(() => caches.match('/offline.html')));
    return;
  }

  // Cache-first for static /css|/js|/images|/lib with ok-only put
  const isStatic =
    url.pathname.startsWith('/css/') ||
    url.pathname.startsWith('/js/') ||
    url.pathname.startsWith('/images/') ||
    url.pathname.startsWith('/lib/');
  if (isStatic) {
    event.respondWith(
      caches.match(req).then((cached) => {
        if (cached && cached.ok) return cached;
        return fetch(req).then((res) => {
          if (res.ok) {
            const copy = res.clone();
            caches.open(CACHE).then((c) => c.put(req, copy)).catch(() => {});
          }
          return res;
        });
      })
    );
    return;
  }

  // Everything else: network-only; offline → offline.html
  event.respondWith(fetch(req).catch(() => caches.match('/offline.html')));
});
