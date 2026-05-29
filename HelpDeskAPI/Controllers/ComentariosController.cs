using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HelpDeskAPI.Data;
using HelpDeskAPI.Models;
using HelpDeskAPI.DTOs;

namespace HelpDeskAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComentariosController : ControllerBase
    {
        private readonly HelpDeskContext _context;

        public ComentariosController(HelpDeskContext context)
        {
            _context = context;
        }

        // ============================================
        // OBTENER COMENTARIOS DE UN TICKET
        // ============================================
        [HttpGet("ticket/{ticketId}")]
        public async Task<IActionResult> ObtenerComentariosPorTicket(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);

            if (ticket == null)
                return NotFound(new { message = "Ticket no encontrado" });

            var permiso = ValidarAccesoAlTicket(ticket);
            if (permiso != null)
                return permiso;

            var comentariosDb = await _context.Comentarios
                .Where(c => c.TicketId == ticketId)
                .Include(c => c.Usuario)
                    .ThenInclude(u => u.UsuarioRoles)
                        .ThenInclude(ur => ur.Rol)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            var comentarios = comentariosDb.Select(c => new ComentarioResponseDto
            {
                Id = c.Id,
                Mensaje = c.Mensaje,
                Fecha = c.Fecha,
                TicketId = c.TicketId,
                UsuarioId = c.UsuarioId,
                UsuarioNombre = c.Usuario?.Nombre ?? "",
                UsuarioRol = c.Usuario != null
                    ? ObtenerRolPrioritario(c.Usuario)
                    : ""
            }).ToList();

            return Ok(comentarios);
        }

        // ============================================
        // CREAR NUEVO COMENTARIO
        // ============================================
        [HttpPost]
        public async Task<IActionResult> CrearComentario([FromBody] CrearComentarioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = ObtenerUsuarioIdActual();

            if (!userId.HasValue)
                return Unauthorized(new { message = "Token inválido" });

            var ticket = await _context.Tickets.FindAsync(dto.TicketId);

            if (ticket == null)
                return NotFound(new { message = "Ticket no encontrado" });

            var permiso = ValidarAccesoAlTicket(ticket);

            if (permiso != null)
                return permiso;

            if (string.IsNullOrWhiteSpace(dto.Mensaje))
                return BadRequest(new { message = "El mensaje no puede estar vacío" });

            var comentario = new Comentario
            {
                Mensaje = dto.Mensaje.Trim(),
                Fecha = DateTime.UtcNow,
                TicketId = dto.TicketId,
                UsuarioId = userId.Value
            };

            _context.Comentarios.Add(comentario);
            await _context.SaveChangesAsync();

            var usuario = await _context.Usuarios
                .Include(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            return Ok(new ComentarioResponseDto
            {
                Id = comentario.Id,
                Mensaje = comentario.Mensaje,
                Fecha = comentario.Fecha,
                TicketId = comentario.TicketId,
                UsuarioId = comentario.UsuarioId,
                UsuarioNombre = usuario?.Nombre ?? "",
                UsuarioRol = usuario != null
                    ? ObtenerRolPrioritario(usuario)
                    : ""
            });
        }

        // ============================================
        // ELIMINAR COMENTARIO
        // ============================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarComentario(int id)
        {
            var userId = ObtenerUsuarioIdActual();

            if (!userId.HasValue)
                return Unauthorized(new { message = "Token inválido" });

            var comentario = await _context.Comentarios.FindAsync(id);

            if (comentario == null)
                return NotFound(new { message = "Comentario no encontrado" });

            var userRole = ObtenerRolActual();

            var puedeEliminar =
                comentario.UsuarioId == userId.Value ||
                userRole == "Admin";

            if (!puedeEliminar)
                return Forbid();

            _context.Comentarios.Remove(comentario);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Comentario eliminado correctamente" });
        }

        // ============================================
        // OBTENER USUARIO ACTUAL DESDE JWT
        // ============================================
        private int? ObtenerUsuarioIdActual()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out int userId))
                return userId;

            return null;
        }

        // ============================================
        // OBTENER ROL ACTUAL DESDE JWT
        // ============================================
        private string? ObtenerRolActual()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        // ============================================
        // PRIORIDAD DE ROLES
        // ============================================
        private string ObtenerRolPrioritario(Usuario usuario)
        {
            var roles = usuario.UsuarioRoles
                .Select(ur => ur.Rol.Nombre)
                .ToList();

            if (roles.Contains("Admin"))
                return "Admin";

            if (roles.Contains("Tecnico"))
                return "Tecnico";

            if (roles.Contains("Solicitante"))
                return "Solicitante";

            return roles.FirstOrDefault() ?? "";
        }

        // ============================================
        // VALIDAR ACCESO AL TICKET
        // ============================================
        private IActionResult? ValidarAccesoAlTicket(Ticket ticket)
        {
            var userId = ObtenerUsuarioIdActual();

            if (!userId.HasValue)
                return Unauthorized(new { message = "Token inválido" });

            var userRole = ObtenerRolActual();

            bool tieneAcceso =
                ticket.UsuarioId == userId.Value ||
                ticket.TecnicoAsignadoId == userId.Value ||
                userRole == "Admin";

            if (!tieneAcceso)
                return Forbid();

            return null;
        }
    }
}