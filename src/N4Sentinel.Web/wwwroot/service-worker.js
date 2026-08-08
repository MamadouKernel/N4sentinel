// N4 Sentinel — Service Worker (PWA).
//
// Stratégie : RÉSEAU D'ABORD, cache uniquement en repli hors ligne.
//
// Un cache-first sur ce projet casse l'application : le document HTML servi par Blazor contient les
// URL versionnées des feuilles de style (`@Assets["app.css"]` -> `app.<empreinte>.css`). Servir un
// HTML mis en cache renvoie donc vers une empreinte qui n'existe plus côté serveur ; la requête CSS
// retombe sur la page 404 (Content-Type text/html), le navigateur la refuse, et l'application
// s'affiche entièrement sans style. C'est exactement le défaut observé le 2026-08-07.
//
// Les requêtes de navigation et les points d'entrée Blazor/SignalR ne sont jamais mis en cache :
// l'application est en Blazor Server et nécessite un circuit vivant, une coquille HTML figée n'a
// aucune valeur d'usage.

const CACHE_NAME = 'n4sentinel-v2';

// Rien n'est pré-mis en cache : les empreintes changent à chaque build, une liste figée serait
// périmée dès la publication suivante. Le cache se remplit au fil des réponses réussies.
self.addEventListener('install', () => {
    // Remplace immédiatement un service worker précédent (notamment le v1 en cache-first).
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        const names = await caches.keys();
        await Promise.all(names.filter(n => n !== CACHE_NAME).map(n => caches.delete(n)));
        await self.clients.claim();
    })());
});

/** Ressources qui ne doivent jamais transiter par le cache. */
function isBypassed(request, url) {
    return request.method !== 'GET'
        || request.mode === 'navigate'
        || url.pathname.startsWith('/_blazor')
        || url.pathname.startsWith('/_framework')
        || url.pathname.startsWith('/Account')
        || url.pathname.startsWith('/reports');
}

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);

    if (url.origin !== self.location.origin || isBypassed(event.request, url)) {
        return; // laisse le navigateur gérer normalement
    }

    event.respondWith((async () => {
        try {
            const response = await fetch(event.request);
            if (response && response.ok) {
                const cache = await caches.open(CACHE_NAME);
                cache.put(event.request, response.clone());
            }
            return response;
        } catch {
            // Hors ligne : on se rabat sur la dernière copie connue, si elle existe.
            const cached = await caches.match(event.request);
            if (cached) {
                return cached;
            }
            throw new Error('Ressource indisponible hors ligne.');
        }
    })());
});
