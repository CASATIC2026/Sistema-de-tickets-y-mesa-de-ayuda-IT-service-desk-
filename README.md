# Path Support 🛠️ — Sistema de Mesa de Ayuda y Gestión de Incidentes

**Path Support** es una solución web integral diseñada para centralizar, estructurar y resolver incidencias o requerimientos técnicos dentro de una organización. El sistema optimiza todo el ciclo de vida de un ticket de soporte, facilitando canales de comunicación directa y organizada entre los usuarios finales y el equipo técnico.

---

## 🚀 Arquitectura y Stack Tecnológico

Este ecosistema ha sido desarrollado priorizando el uso de tecnologías nativas y arquitecturas limpias, prescindiendo intencionalmente de frameworks pesados en el cliente para garantizar un rendimiento óptimo, código ligero y un control total sobre el flujo de datos:

* **Backend:** .NET 8.0 (`C#` Puro) con inyección de dependencias y arquitectura orientada a servicios.
* **Base de Datos:** `PostgreSQL` para una persistencia relacional sólida con integridad referencial.
* **Frontend:** `HTML5` Semántico, `CSS3` personalizado y `Vanilla JavaScript` (JS Puro para manipulación dinámica del DOM sin librerías mágicas).
* **Infraestructura:** `Docker` y `Docker Compose` para el aislamiento completo del entorno, facilitando el despliegue multiplataforma.

---

## 👥 Flujo de Trabajo y Modos de Uso por Rol

La interfaz de usuario y los privilegios de la API cambian dinámicamente según el rol de la cuenta autenticada:

### 1. Panel del Solicitante (Usuario Final)
* **Objetivo:** Permitir a los usuarios reportar problemas sin fricciones.
* **¿Cómo funciona?:** El usuario inicia sesión y visualiza una pantalla limpia con un botón centralizado para **Crear nuevo ticket**. Puede llenar los detalles de su incidencia y seguir el estado de sus requerimientos en tiempo real en una tabla histórica con indicadores visuales de prioridad y niveles de servicio (SLA).

### 2. Panel del Administrador (Gestión Operativa)
* **Objetivo:** Monitorear la operación global y equilibrar la carga de trabajo.
* **¿Cómo funciona?:** El administrador accede a un dashboard analítico completo con contadores en tiempo real (Tickets Totales, Abiertos, En Progreso, Resueltos, Vencidos). Cuenta con accesos rápidos para inspeccionar la base de datos de usuarios y tiene la facultad exclusiva de **asignar y delegar** los tickets entrantes a los técnicos disponibles.

### 3. Panel del Técnico (Soporte Técnico)
* **Objetivo:** Resolver incidentes de manera eficiente.
* **¿Cómo funciona?:** El técnico cuenta con una bandeja de entrada automatizada que muestra únicamente los casos que tiene bajo su responsabilidad. Al interactuar con un ticket, el sistema abre un espacio de conversación fluida con el solicitante para coordinar los pasos de la solución hasta el cierre definitivo de la incidencia.

---





