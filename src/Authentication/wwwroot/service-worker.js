const staticCacheName = 'precache-v1.1.0';
const dynamicCacheName = 'runtimeCache-v1.1.0';


console.log('Hello from service-worker.js');


// Pre Caching Assets
const precacheAssets = [
    'pwa.js',
    'manifest.json',
    'lib/jquery/jquery.js',
    'lib/bootstrap/dist/js/bootstrap.min.js',
    'js/site.js',
    'js/offline.js',
    'js/jsnlog.min.js',

    'offline',

    'css/site.css',
    'css/bootstrap.custom.min.css',

    'logo.svg',
    'favicon.ico',
    'img/offline.png'
];

// Install Event
self.addEventListener('install', function (event) {
    event.waitUntil(
        caches.open(staticCacheName).then(function (cache) {
            return cache.addAll(precacheAssets);
        })
    );
});

// Activate Event
self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys().then(keys => {
            return Promise.all(keys
                .filter(key => key !== staticCacheName && key !== dynamicCacheName)
                .map(key => caches.delete(key))
            );
        })
    );
});

// Fetch Event
self.addEventListener('fetch', function (event) {
    event.respondWith(
        caches.match(event.request).then(cacheRes => {
            return cacheRes || fetch(event.request).then(response => {
                return response;
            }).catch(function () {

                if (event.request.method === "GET" && event.request.destination === "document")
                    return caches.match('offline'); // Fallback Page, When No Internet Connection

                return null;
            });
        }).catch(function () {

            if (event.request.method === "GET" && event.request.destination === "document")
                return caches.match('offline'); // Fallback Page, When No Internet Connection

            return null;
        })
    );
});
