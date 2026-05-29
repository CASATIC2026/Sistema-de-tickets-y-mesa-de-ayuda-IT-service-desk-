/**
 * ======================================================
 * FUNCIONES BASE - Mesa de Ayuda
 * ======================================================
 * Maneja sesión JWT, validación, redirección por rol,
 * notificaciones y utilidades de formato.
 *
 * Depende de:  <script src=".../js/config.js"></script>
 * que define la constante global  window.API_BASE_URL
 * ======================================================
 */

// Fallback defensivo si por alguna razón config.js no fue cargado
if (typeof window.API_BASE_URL === 'undefined') {
    console.warn('[funcionBase] window.API_BASE_URL no definido; usando fallback LAN dinamico');
    window.API_BASE_URL = `http://${window.location.hostname}:8080`;
}

/* ============================ TOKEN ============================ */
function getToken() {
    return (
        localStorage.getItem('token') ||
        sessionStorage.getItem('token') ||
        null
    );
}

function getCurrentUser() {
    return {
        id: parseInt(
            localStorage.getItem('usuarioId') ||
            sessionStorage.getItem('usuarioId') ||
            '0', 10),
        rol:    localStorage.getItem('rol')    || sessionStorage.getItem('rol')    || '',
        nombre: localStorage.getItem('nombre') || sessionStorage.getItem('nombre') || '',
        correo: localStorage.getItem('correo') || sessionStorage.getItem('correo') || ''
    };
}

function isAuthenticated() {
    const t = getToken();
    const u = getCurrentUser();
    return !!(t && u.id > 0 && u.rol && u.nombre);
}

/* ============================ VALIDACIÓN SERVER ============================ */
async function validateSession() {
    const token = getToken();
    if (!token) { logout(); return false; }

    try {
        const response = await fetch(`${window.API_BASE_URL}/api/Auth/me`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) { logout(); return false; }

        const usuario = await response.json();
        if (!usuario || !usuario.id || !usuario.rol) { logout(); return false; }

        // Resincronizar identidad (anti-tamper local)
        ['localStorage', 'sessionStorage'].forEach(store => {
            window[store].setItem('usuarioId', usuario.id);
            window[store].setItem('rol',       usuario.rol);
            window[store].setItem('nombre',    usuario.nombre || '');
            window[store].setItem('correo',    usuario.correo || '');
        });

        return true;
    } catch (err) {
        console.error('Error validando sesión:', err);
        logout();
        return false;
    }
}

/* ============================ HEADERS / FETCH ============================ */
function getAuthHeaders() {
    const headers = { 'Content-Type': 'application/json' };
    const token = getToken();
    if (token) headers['Authorization'] = `Bearer ${token}`;
    return headers;
}

async function fetchWithAuth(url, options = {}) {
    const merged = {
        ...options,
        headers: { ...getAuthHeaders(), ...(options.headers || {}) }
    };

    try {
        const response = await fetch(url, merged);
        
        if (response.status === 401 || response.status === 403) {
            console.warn("Token inválido o expirado detectado por la API. Forzando salida.");
            logout();
            // Retornamos una promesa que nunca se resuelve para congelar los fetches 
            // subsiguientes y evitar que los archivos HTML individuales sigan tirando errores
            return new Promise(() => {}); 
        }
        return response;
    } catch (error) {
        console.error("Error de red o conexión con la API:", error);
        throw error;
    }
}

/* ============================ ROL / EXPIRACIÓN ============================ */
function getUserRole() {
    return localStorage.getItem('rol') || sessionStorage.getItem('rol') || '';
}

function tokenExpired() {
    const exp = localStorage.getItem('tokenExpiration') || sessionStorage.getItem('tokenExpiration');
    if (!exp) return false; // Cambiado a false temporalmente para evitar falsos bloqueos si no manejas expiración estricta
    return new Date(exp) < new Date();
}

/* ============================ LOGOUT REFORZADO ============================ */
function logout() {
    console.log("Iniciando proceso de cierre de sesión seguro...");

    localStorage.clear();
    sessionStorage.clear();

    localStorage.removeItem('token');
    localStorage.removeItem('rol');
    localStorage.removeItem('tokenExpiration');
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('rol');
    sessionStorage.removeItem('tokenExpiration');

    setTimeout(() => {
        console.log("Almacenamiento limpio. Redirigiendo al Login de forma segura.");
        window.location.replace('../solicitante/Login.html'); 
    }, 100);
}

/* ============================ REDIRECCIÓN ============================ */

// Usamos window para blindar la variable contra redeclaraciones de ámbito en el navegador
if (typeof window.redireccionEnProceso === 'undefined') {
    window.redireccionEnProceso = false;
}

function redirectByRole(rol) {
    if (window.redireccionEnProceso) {
        console.log("Redirección bloqueada para evitar loop");
        return;
    }

    const paginaActual = window.location.pathname.toLowerCase();
    let destino = '';

    const rolNormalizado = (rol || '').trim().toLowerCase();

    switch (rolNormalizado) {
        case 'admin':
            destino = '/administrador/dashboard.html';
            break;

        case 'tecnico':
            destino = '/administrador/tecnico.html';
            break;

        case 'solicitante':
        default:
            destino = '/solicitante/inicio.html';
            break;
    }

    // Comprobación exacta al final de la ruta del navegador para evitar loops
    if (paginaActual.endsWith(destino.toLowerCase())) {
        console.log("Ya estamos en la página correcta:", destino);
        return;
    }

    window.redireccionEnProceso = true;
    console.log("Redirigiendo hacia:", destino);
    window.location.href = window.location.origin + destino;
}


/* ============================ PROTECCIÓN REFORZADA ============================ */
async function protectPage() {
    const paginaActual = window.location.pathname.toLowerCase();
    
    if (paginaActual.includes("login.html")) {
        console.log("Estamos en el Login, deteniendo protectPage para evitar bucle.");
        return; 
    }

    if (!isAuthenticated() || tokenExpired()) { 
        logout(); 
        return; 
    }
    await validateSession();
}

/* ============================ FORMATO ============================ */
function formatDate(dateString) {
    if (!dateString) return '-';
    const d = new Date(dateString);
    return d.toLocaleDateString('es-ES', {
        day: '2-digit', month: '2-digit', year: 'numeric',
        hour: '2-digit', minute: '2-digit'
    });
}

function getSlaClass(slaStatus) {
    switch (slaStatus) {
        case 'Vencido':    return 'sla-danger';
        case 'Por vencer': return 'sla-warning';
        case 'En tiempo':
        case 'Cumplido':   return 'sla-normal';
        default:           return '';
    }
}

function getPriorityClass(prioridad) {
    switch ((prioridad || '').toLowerCase()) {
        case 'alta':
        case 'critico': return 'priority-high';
        case 'media':   return 'priority-medium';
        case 'baja':    return 'priority-low';
        default:        return 'priority-medium';
    }
}

function getStatusClass(estado) {
    switch (estado) {
        case 'Abierto':     return 'status-open';
        case 'En Progreso': return 'status-progress';
        case 'Resuelto':
        case 'Cerrado':     return 'status-closed';
        default:            return '';
    }
}

/* ============================ NOTIFICACIONES ============================ */
function showNotification(message, type = 'info') {
    const n = document.createElement('div');
    n.className = `notification notification-${type}`;
    n.textContent = message;
    n.style.cssText = `
        position: fixed; top: 20px; right: 20px;
        padding: 15px 25px; border-radius: 8px; color: white;
        font-weight: 500; z-index: 10000;
        animation: slideIn 0.3s ease; max-width: 90%;
    `;
    const colors = {
        success: '#10b981', error: '#ef4444',
        warning: '#f59e0b', info: '#3b82f6'
    };
    n.style.backgroundColor = colors[type] || colors.info;
    document.body.appendChild(n);
    setTimeout(() => {
        n.style.animation = 'slideOut 0.3s ease';
        setTimeout(() => n.remove(), 300);
    }, 3000);
}

/* ============================ DOM READY UNIFICADO ============================ */
document.addEventListener('DOMContentLoaded', async function () {
    
    const esLogin = window.location.pathname.toLowerCase().includes('login.html');

    if (!esLogin) {
        await protectPage();
    } else {
        const rolGuardado = localStorage.getItem('rol') || sessionStorage.getItem('rol');
        if (rolGuardado && isAuthenticated() && !tokenExpired()) {
            console.log("Usuario ya autenticado con rol: " + rolGuardado + ". Redirigiendo...");
            redirectByRole(rolGuardado);
        }
    }

    const logoutBtn = document.getElementById('logoutBtn') || document.getElementById('cerrarSesionBtn');
    if (logoutBtn) {
        logoutBtn.removeEventListener('click', logout);
        logoutBtn.addEventListener('click', logout);
    }

    // FAQ logic unificada en un solo lugar limpio
    document.querySelectorAll('.faq-question').forEach(button => {
        button.addEventListener('click', function () {
            const answer = this.nextElementSibling;
            const parent = this.parentElement;

            document.querySelectorAll('.faq-answer').forEach(item => {
                if (item !== answer) {
                    item.style.display = 'none';
                    item.parentElement?.classList.remove('active');
                }
            });

            if (answer.style.display === 'block') {
                answer.style.display = 'none';
                parent?.classList.remove('active');
            } else {
                answer.style.display = 'block';
                parent?.classList.add('active');
            }
        });
    });
});

/* ============================ STYLES ============================ */
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn  { from { transform: translateX(100%); opacity: 0; } to { transform: translateX(0); opacity: 1; } }
    @keyframes slideOut { from { transform: translateX(0); opacity: 1; } to { transform: translateX(100%); opacity: 0; } }
`;
document.head.appendChild(style);