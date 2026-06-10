
if (typeof window.API_BASE_URL === 'undefined') {
    console.warn('[funcionBase] window.API_BASE_URL no definido; usando fallback LAN dinamico');
    window.API_BASE_URL = `http://${window.location.hostname}:8080`;
}

function getToken() {
    return localStorage.getItem('token') || sessionStorage.getItem('token') || null;
}

function getCurrentUser() {
    return {
        id: parseInt(localStorage.getItem('usuarioId') || sessionStorage.getItem('usuarioId') || '0', 10),
        rol:    localStorage.getItem('rol')    || sessionStorage.getItem('rol')    || '',
        nombre: localStorage.getItem('nombre') || sessionStorage.getItem('nombre') || '',
        correo: localStorage.getItem('correo') || sessionStorage.getItem('correo') || ''
    };
}

function getAuthHeaders(extraHeaders = {}) {
    const token = getToken();
    return {
        'Content-Type': 'application/json',
        ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
        ...extraHeaders
    };
}

window.fetchWithAuth = async function fetchWithAuth(url, options = {}) {
    const token = getToken();
    return fetch(url, {
        ...options,
        headers: {
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
            ...(options.headers || {})
        }
    });
};

window.REFRESH_EVENT_KEY = 'helpdesk-data-refresh';

window.notifyDataChanged = function(scope = 'tickets', ticketId = null) {
    localStorage.setItem(window.REFRESH_EVENT_KEY, JSON.stringify({
        scope,
        ticketId,
        timestamp: Date.now()
    }));
};

window.escapeHtml = function(text) {
    return (text || '').replace(/[&<>"']/g, char => ({
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#39;'
    }[char]));
};

window.renderChatMessages = function(containerId, comentarios, usuarioIdActual, miRolActual) {
    const contenedor = document.getElementById(containerId);
    if (!contenedor) return;

    const estabaAbajo = contenedor.scrollHeight - contenedor.scrollTop <= contenedor.clientHeight + 60;
    contenedor.innerHTML = '';

    if (!comentarios || comentarios.length === 0) {
        contenedor.innerHTML = `<p style="color: #6b7280; text-align: center; padding: 20px;">No hay mensajes aún.</p>`;
        return;
    }

    comentarios.forEach(com => {
        const div = document.createElement('div');
        div.classList.add('comment');
        
        const idDuenoMensaje = Number(com.usuarioId);
        const esPropio = usuarioIdActual && idDuenoMensaje === usuarioIdActual;
        const esSoporte = com.usuarioRol === 'Admin' || com.usuarioRol === 'Tecnico';
        const yoSoySoporte = miRolActual === 'Admin' || miRolActual === 'Tecnico';

        if (esPropio) {
            div.classList.add('own');
        } else if (yoSoySoporte && esSoporte) {
            div.classList.add('own');
        } else if (esSoporte) {
            div.classList.add('tecnico');
        }

        const fechaStr = com.fecha ? new Date(com.fecha).toLocaleString('es-ES') : '';
        const tagRol = com.usuarioRol ? ` (${com.usuarioRol})` : '';

        div.innerHTML = `
            <div class="comment-header">
                <strong>${window.escapeHtml(com.usuarioNombre)}${tagRol}</strong>
                <span>${fechaStr}</span>
            </div>
            <p>${window.escapeHtml(com.mensaje)}</p>
        `;
        contenedor.appendChild(div);
    });

    if (estabaAbajo) {
        contenedor.scrollTop = contenedor.scrollHeight;
    }
};

function isAuthenticated() {
    const t = getToken();
    const u = getCurrentUser();
    return !!(t && u.id > 0 && u.rol && u.nombre);
}

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

function logout(event) {
    if (event && typeof event.preventDefault === 'function') {
        event.preventDefault();
    }
    
    console.log("Cerrando sesión de forma definitiva...");

    localStorage.clear();
    sessionStorage.clear();

    const loginDestino = window.location.origin + '/solicitante/Login.html';
    console.log("Redirigiendo a:", loginDestino);
    window.location.replace(loginDestino); 
}

if (typeof window.redireccionEnProceso === 'undefined') {
    window.redireccionEnProceso = false;
}

function redirectByRole(rol) {
    if (window.redireccionEnProceso) return;

    const paginaActual = window.location.pathname.toLowerCase();
    let destino = '';
    const rolNormalizado = (rol || '').trim().toLowerCase();

    switch (rolNormalizado) {
        case 'admin':      destino = '/administrador/dashboard.html'; break;
        case 'tecnico':    destino = '/administrador/tecnico.html'; break;
        case 'solicitante':
        default:           destino = '/solicitante/inicio.html'; break;
    }

    if (paginaActual.endsWith(destino.toLowerCase())) {
        return;
    }

    window.redireccionEnProceso = true;
    window.location.href = window.location.origin + destino;
}

async function protectPage() {
    const paginaActual = window.location.pathname.toLowerCase();
    
    if (paginaActual.includes("login.html")) {
        return; 
    }

    if (!isAuthenticated()) { 
        logout(); 
        return; 
    }
    await validateSession();
}

function formatDate(dateString) {
    if (!dateString) return '-';
    const d = new Date(dateString);
    return d.toLocaleDateString('es-ES', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

function getSlaClass(slaStatus) {
    if (slaStatus === 'Vencido') return 'sla-danger';
    if (slaStatus === 'Por vencer') return 'sla-warning';
    if (['En tiempo', 'Cumplido'].includes(slaStatus)) return 'sla-normal';
    return '';
}

function getPriorityClass(prioridad) {
    const p = (prioridad || '').toLowerCase();
    if (['alta', 'critico'].includes(p)) return 'priority-high';
    if (p === 'baja') return 'priority-low';
    return 'priority-medium';
}

function getStatusClass(estado) {
    if (estado === 'Abierto') return 'status-open';
    if (estado === 'En Progreso') return 'status-progress';
    if (['Resuelto', 'Cerrado'].includes(estado)) return 'status-closed';
    return '';
}

function showNotification(message, type = 'info') {
    const n = document.createElement('div');
    n.className = `notification notification-${type}`;
    n.textContent = message;
    n.style.cssText = `
        position: fixed; top: 20px; right: 20px; padding: 15px 25px; border-radius: 8px; color: white;
        font-weight: 500; z-index: 10000; animation: slideIn 0.3s ease; max-width: 90%;
    `;
    const colors = { success: '#10b981', error: '#ef4444', warning: '#f59e0b', info: '#3b82f6' };
    n.style.backgroundColor = colors[type] || colors.info;
    document.body.appendChild(n);
    setTimeout(() => {
        n.style.animation = 'slideOut 0.3s ease';
        setTimeout(() => n.remove(), 300);
    }, 3000);
}

document.addEventListener('DOMContentLoaded', async function () {
    const esLogin = window.location.pathname.toLowerCase().includes('login.html');

    if (!esLogin) {
        await protectPage();
    } else {
        const rolGuardado = localStorage.getItem('rol') || sessionStorage.getItem('rol');
        if (rolGuardado && isAuthenticated()) {
            redirectByRole(rolGuardado);
        }
    }

    const botonesLogout = ['logoutBtn', 'cerrarSesionBtn'];
    botonesLogout.forEach(id => {
        const btn = document.getElementById(id);
        if (btn) {
            btn.removeAttribute('onclick'); 
            btn.addEventListener('click', logout);
        }
    });

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

{
    const estilosNotificacion = document.createElement('style');
    estilosNotificacion.textContent = `
        @keyframes slideIn  { from { transform: translateX(100%); opacity: 0; } to { transform: translateX(0); opacity: 1; } }
        @keyframes slideOut { from { transform: translateX(0); opacity: 1; } to { transform: translateX(100%); opacity: 0; } }
    `;
    document.head.appendChild(estilosNotificacion);
}
