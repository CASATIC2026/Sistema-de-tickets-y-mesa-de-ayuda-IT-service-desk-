/**
 * ======================================================
 * CONFIGURACIÓN GLOBAL DEL FRONTEND
 * ======================================================
 *
 * Edita este archivo para cambiar la URL del API en un solo lugar.
 * Soporta multi-entorno por hostname automáticamente.
 *
 * No incluir credenciales aquí.
 * ======================================================
 */
(function () {
    const host = window.location.hostname;
    const railwayApiUrl = 'https://helpdeskapi-production-ad7f.up.railway.app';

    const apiPort = window.API_PORT || '8080';
    let apiBaseUrl = `http://${host}:${apiPort}`;

    // Cualquier frontend hospedado en Vercel debe hablar con Railway.
    if (host.endsWith('.vercel.app')) {
        apiBaseUrl = railwayApiUrl;
    }

    // Permite definir overrides por hostname (ej. dominios de prod / staging)
    const overrides = {
        '192.168.204.82': 'http://192.168.204.82:8080',
        'localhost': 'http://192.168.204.82:8080'
    };

    if (overrides[host]) {
        apiBaseUrl = overrides[host];
    }

    // Permite override en runtime (consola dev): window.__API_BASE_URL = '...'
    if (typeof window.__API_BASE_URL === 'string' && window.__API_BASE_URL.length > 0) {
        apiBaseUrl = window.__API_BASE_URL;
    }

    window.API_BASE_URL = apiBaseUrl;
})();
