// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
// Do not precache .webmanifest with SRI: Azure/Linux often serves wrong bytes (404 HTML, gzip/br mismatch), which fails
// cache.addAll integrity checks and breaks the whole service worker install. The app manifest is still loaded via <link rel="manifest">.
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

function isOfflineStaticPath(pathname) {
    const p = pathname.toLowerCase();
    return p.startsWith('/_framework/') ||
        p.startsWith('/_content/') ||
        p.startsWith('/css/') ||
        p.startsWith('/service-worker') ||
        /\.(wasm|dll|pdb|json|css|js|map|png|jpg|jpeg|gif|svg|webp|ico|woff2?|blat|dat|html|webmanifest)$/i.test(pathname);
}

async function onFetch(event) {
    if (event.request.method !== 'GET') {
        return fetch(event.request);
    }

    const url = new URL(event.request.url);
    if (url.origin !== self.location.origin) {
        return fetch(event.request);
    }

    const cache = await caches.open(cacheName);
    let cachedResponse = await cache.match(event.request);
    if (cachedResponse) {
        return cachedResponse;
    }

    // Blazor client routes (/game/{id}, /login, …) are not real files. The stock template only treated
    // mode === "navigate" as SPA shell; other GETs (prefetch, Sec-Fetch-Mode variants) hit fetch(/game/...)
    // and fail with "TypeError: Failed to fetch" under the service worker. Serve index.html instead.
    const wantsSpaShell =
        !isOfflineStaticPath(url.pathname) &&
        !manifestUrlList.some(u => u === event.request.url) &&
        !url.pathname.toLowerCase().startsWith('/api/');

    const indexHref = new URL('index.html', self.location.origin).href;
    if (wantsSpaShell) {
        cachedResponse = await cache.match(indexHref)
            || await cache.match('index.html')
            || await cache.match(new URL('/index.html', self.location.origin).href);
        if (cachedResponse) {
            return cachedResponse;
        }
        return fetch(new Request(indexHref, { cache: 'reload', credentials: 'same-origin' }));
    }

    return fetch(event.request);
}
