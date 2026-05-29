
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HelpDeskAPI.Models;
using HelpDeskAPI.DTOs;
using HelpDeskAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HelpDeskAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly HelpDeskContext _context;

        public TicketsController(HelpDeskContext context)
        {
            _context = context;
        }

        [HttpGet("mis-tickets")]
        [Authorize]
        public async Task<IActionResult> ObtenerMisTickets()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var tickets = await _context.Tickets
                .Where(t => t.UsuarioId == userId)
                .Include(t => t.TecnicoAsignado)
                .Include(t => t.Categoria)
                .OrderByDescending(t => t.FechaCreacion)
                .Select(t => new TicketResponseDto
                {
                    Id = t.Id,
                    Titulo = t.Titulo,
                    Descripcion = t.Descripcion,
                    Prioridad = t.Prioridad,
                    Estado = t.Estado,
                    FechaCreacion = t.FechaCreacion,
                    FechaLimite = t.FechaLimite,
                    FechaResolucion = t.FechaResolucion,
                    UsuarioId = t.UsuarioId,
                    TecnicoAsignadoId = t.TecnicoAsignadoId,
                    TecnicoNombre = t.TecnicoAsignado != null ? t.TecnicoAsignado.Nombre : null,
                    CategoriaId = t.CategoriaId,
                    CategoriaNombre = t.Categoria != null ? t.Categoria.Nombre : null,
                    SlaStatus = t.SlaStatus
                })
                .ToListAsync();

            return Ok(tickets);
        }

        [HttpGet("usuario/{usuarioId}")]
        [Authorize]
        public async Task<IActionResult> ObtenerTicketsPorUsuario(int usuarioId)
        {
            var permiso = ObtenerPermisoConsultaTickets();
            if (permiso != null)
                return permiso;

            var userId = ObtenerUsuarioIdActual();
            var userRole = ObtenerRolActual();

            if (userRole != "Admin" && userId != usuarioId)
                return Forbid();

            var tickets = await _context.Tickets
                .Where(t => t.UsuarioId == usuarioId)
                .Include(t => t.TecnicoAsignado)
                .Include(t => t.Categoria)
                .OrderByDescending(t => t.FechaCreacion)
                .Select(t => new TicketResponseDto
                {
                    Id = t.Id,
                    Titulo = t.Titulo,
                    Descripcion = t.Descripcion,
                    Prioridad = t.Prioridad,
                    Estado = t.Estado,
                    FechaCreacion = t.FechaCreacion,
                    FechaLimite = t.FechaLimite,
                    FechaResolucion = t.FechaResolucion,
                    UsuarioId = t.UsuarioId,
                    TecnicoAsignadoId = t.TecnicoAsignadoId,
                    TecnicoNombre = t.TecnicoAsignado != null ? t.TecnicoAsignado.Nombre : null,
                    CategoriaId = t.CategoriaId,
                    CategoriaNombre = t.Categoria != null ? t.Categoria.Nombre : null,
                    SlaStatus = t.SlaStatus
                })
                .ToListAsync();

            return Ok(tickets);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ObtenerTodos()
        {
            var permiso = ObtenerPermisoConsultaTickets();
            if (permiso != null)
                return permiso;

            var userId = ObtenerUsuarioIdActual();
            var userRole = ObtenerRolActual();

            var query = _context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.TecnicoAsignado)
                .Include(t => t.Categoria)
                .AsQueryable();

            if (userRole == "Tecnico")
                query = query.Where(t => t.TecnicoAsignadoId == userId);
            else if (userRole != "Admin")
                query = query.Where(t => t.UsuarioId == userId);

            var tickets = await query
                .OrderByDescending(t => t.FechaCreacion)
                .Select(t => new TicketResponseDto
                {
                    Id = t.Id,
                    Titulo = t.Titulo,
                    Descripcion = t.Descripcion,
                    Prioridad = t.Prioridad,
                    Estado = t.Estado,
                    FechaCreacion = t.FechaCreacion,
                    FechaLimite = t.FechaLimite,
                    FechaResolucion = t.FechaResolucion,
                    UsuarioId = t.UsuarioId,
                    UsuarioNombre = t.Usuario != null ? t.Usuario.Nombre : null,
                    TecnicoAsignadoId = t.TecnicoAsignadoId,
                    TecnicoNombre = t.TecnicoAsignado != null ? t.TecnicoAsignado.Nombre : null,
                    CategoriaId = t.CategoriaId,
                    CategoriaNombre = t.Categoria != null ? t.Categoria.Nombre : null,
                    SlaStatus = t.SlaStatus
                })
                .ToListAsync();

            return Ok(tickets);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.TecnicoAsignado)
                    .ThenInclude(u => u!.UsuarioRoles)
                        .ThenInclude(ur => ur.Rol)
                .Include(t => t.Categoria)
                .Include(t => t.Comentarios!)
                    .ThenInclude(c => c.Usuario)
                        .ThenInclude(u => u!.UsuarioRoles)
                            .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound(new { message = "Ticket no encontrado" });

            var permiso = ValidarAccesoAlTicket(ticket);
            if (permiso != null)
                return permiso;

            var response = new 
            {
                ticket.Id,
                ticket.Titulo,
                ticket.Descripcion,
                ticket.Prioridad,
                ticket.Estado,
                ticket.FechaCreacion,
                ticket.FechaLimite,
                ticket.FechaResolucion,
                ticket.UsuarioId,
                UsuarioNombre = ticket.Usuario?.Nombre,
                ticket.TecnicoAsignadoId,
                TecnicoNombre = ticket.TecnicoAsignado?.Nombre,
                ticket.CategoriaId,
                CategoriaNombre = ticket.Categoria?.Nombre,
                ticket.SlaStatus,
                Comentarios = ticket.Comentarios?.Select(c => new ComentarioResponseDto
                {
                    Id = c.Id,
                    Mensaje = c.Mensaje,
                    Fecha = c.Fecha,
                    TicketId = c.TicketId,
                    UsuarioId = c.UsuarioId,
                    UsuarioNombre = c.Usuario?.Nombre ?? "",
                    UsuarioRol = c.Usuario != null
                        ? c.Usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).FirstOrDefault() ?? ""
                        : ""
                }).OrderBy(c => c.Fecha).ToList()
            };

            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CrearTicket([FromBody] CrearTicketDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int? usuarioId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int parsedUserId))
            {
                usuarioId = parsedUserId;
            }

            if (!usuarioId.HasValue)
                return Unauthorized(new { message = "Token inválido" });

            var fechaLimite = CalcularFechaLimite(dto.Prioridad);

            var ticket = new Ticket
            {
                Titulo = dto.Titulo.Trim(),
                Descripcion = dto.Descripcion.Trim(),
                Prioridad = dto.Prioridad ?? "Media",
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow,
                FechaLimite = fechaLimite,
                UsuarioId = usuarioId,
                CategoriaId = dto.CategoriaId
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return Ok(new TicketResponseDto
            {
                Id = ticket.Id,
                Titulo = ticket.Titulo,
                Descripcion = ticket.Descripcion,
                Prioridad = ticket.Prioridad,
                Estado = ticket.Estado,
                FechaCreacion = ticket.FechaCreacion,
                FechaLimite = ticket.FechaLimite,
                UsuarioId = ticket.UsuarioId,
                CategoriaId = ticket.CategoriaId,
                SlaStatus = ticket.SlaStatus
            });
        }

        [HttpPost("crear")]
        [Authorize]
        public IActionResult CrearTicketConUsuarioLegacy()
        {
            // [SECURITY] Endpoint legacy deshabilitado: aceptaba UsuarioId arbitrario sin auth.
            return StatusCode(StatusCodes.Status410Gone,
                new { message = "Endpoint deshabilitado. Utilice POST /api/Tickets." });
        }

        [HttpPut("{id}/asignar")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AsignarTicket(int id, [FromBody] AsignarTicketDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return NotFound(new { message = "Ticket no encontrado" });

            var tecnico = await _context.Usuarios
                .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(u => u.Id == dto.TecnicoId && u.Activo &&
                    u.UsuarioRoles.Any(ur => ur.Rol.Nombre == "Tecnico" || ur.Rol.Nombre == "Admin"));

            if (tecnico == null)
                return BadRequest(new { message = "Técnico no encontrado o sin permisos" });

            ticket.TecnicoAsignadoId = dto.TecnicoId;
            
            if (ticket.Estado == "Abierto")
                ticket.Estado = "En Progreso";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Ticket asignado correctamente", tecnicoNombre = tecnico.Nombre });
        }

        [HttpPut("{id}/estado")]
        [Authorize(Roles = "Admin,Tecnico")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return NotFound(new { message = "Ticket no encontrado" });

            var permiso = ValidarAccesoTecnicoOAdminAlTicket(ticket);
            if (permiso != null)
                return permiso;

            ticket.Estado = dto.Estado;

            if (dto.Estado == "Resuelto" || dto.Estado == "Cerrado")
            {
                ticket.FechaResolucion = DateTime.UtcNow;
            }
            else
            {
                ticket.FechaResolucion = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Estado actualizado", nuevoEstado = ticket.Estado, slaStatus = ticket.SlaStatus });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Tecnico")]
        public async Task<IActionResult> ActualizarTicket(int id, [FromBody] ActualizarTicketDto dto)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return NotFound(new { message = "Ticket no encontrado" });

            var permiso = ValidarAccesoTecnicoOAdminAlTicket(ticket);
            if (permiso != null)
                return permiso;

            if (!string.IsNullOrWhiteSpace(dto.Titulo))
                ticket.Titulo = dto.Titulo.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Descripcion))
                ticket.Descripcion = dto.Descripcion.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Prioridad))
            {
                ticket.Prioridad = dto.Prioridad;
                ticket.FechaLimite = CalcularFechaLimite(dto.Prioridad, ticket.FechaCreacion);
            }

            if (!string.IsNullOrWhiteSpace(dto.Estado))
            {
                ticket.Estado = dto.Estado;
                if (dto.Estado == "Resuelto" || dto.Estado == "Cerrado")
                    ticket.FechaResolucion = DateTime.UtcNow;
            }

            if (dto.TecnicoAsignadoId.HasValue)
                ticket.TecnicoAsignadoId = dto.TecnicoAsignadoId;

            if (dto.CategoriaId.HasValue)
                ticket.CategoriaId = dto.CategoriaId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Ticket actualizado correctamente" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EliminarTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return NotFound(new { message = "Ticket no encontrado" });

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ticket eliminado correctamente" });
        }

        [HttpGet("estadisticas")]
        [Authorize(Roles = "Admin,Tecnico")]
        public async Task<IActionResult> ObtenerEstadisticas()
        {
            var permiso = ObtenerPermisoConsultaTickets();
            if (permiso != null)
                return permiso;

            var userId = ObtenerUsuarioIdActual();
            var userRole = ObtenerRolActual();

            var query = _context.Tickets.AsQueryable();
            if (userRole == "Tecnico")
                query = query.Where(t => t.TecnicoAsignadoId == userId);

            var stats = new
            {
                Total = await query.CountAsync(),
                Abiertos = await query.CountAsync(t => t.Estado == "Abierto"),
                EnProgreso = await query.CountAsync(t => t.Estado == "En Progreso"),
                Resueltos = await query.CountAsync(t => t.Estado == "Resuelto"),
                Cerrados = await query.CountAsync(t => t.Estado == "Cerrado"),
                PrioridadAlta = await query.CountAsync(t => t.Prioridad == "Alta"),
                PrioridadMedia = await query.CountAsync(t => t.Prioridad == "Media"),
                PrioridadBaja = await query.CountAsync(t => t.Prioridad == "Baja"),
                SlaVencidos = await query
                    .CountAsync(t => t.FechaLimite < DateTime.UtcNow && 
                        t.Estado != "Resuelto" && t.Estado != "Cerrado")
            };

            return Ok(stats);
        }

        private DateTime CalcularFechaLimite(string? prioridad, DateTime? fechaBase = null)
        {
            var fecha = fechaBase ?? DateTime.UtcNow;
            return prioridad?.ToLower() switch
            {
                "alta" or "critico" => fecha.AddHours(4),
                "media" => fecha.AddHours(8),
                "baja" => fecha.AddHours(24),
                _ => fecha.AddHours(8)
            };
        }

        private int? ObtenerUsuarioIdActual()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }

        private string? ObtenerRolActual()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        private IActionResult? ObtenerPermisoConsultaTickets()
        {
            var userId = ObtenerUsuarioIdActual();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Token inválido" });

            var userRole = ObtenerRolActual();
            if (string.IsNullOrWhiteSpace(userRole))
                return Forbid();

            return null;
        }

        private IActionResult? ValidarAccesoAlTicket(Ticket ticket)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new {message = "Token inválido" });

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var canAccess = userRole == "Admin" ||
                            (userRole == "Tecnico" && ticket.TecnicoAsignadoId == userId) ||
                            ticket.UsuarioId == userId;

            if (!canAccess)
                return Forbid();
                
            return null;
        }

        private IActionResult? ValidarAccesoTecnicoOAdminAlTicket(Ticket ticket)
        {
            var permiso = ObtenerPermisoConsultaTickets();
            if (permiso != null)
                return permiso;

            var userId = ObtenerUsuarioIdActual();
            var userRole = ObtenerRolActual();

            var canAccess = userRole == "Admin" ||
                            (userRole == "Tecnico" && ticket.TecnicoAsignadoId == userId);

            return canAccess ? null : Forbid();
        }
    }
}
