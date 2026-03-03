using Microsoft.AspNetCore.Mvc;
using HelpDeskAPI.Models;

namespace HelpDeskAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        // Simulación de datos (temporal)
        private static List<Ticket> tickets = new List<Ticket>
        {
            new Ticket
            {
                Id = 1,
                Titulo = "Error impresora",
                Descripcion = "No imprime en oficina",
                Prioridad = "Alta",
                Estado = "Abierto",
                UsuarioId = 1
            }
        };

        // GET: api/tickets/usuario/1
        [HttpGet("usuario/{usuarioId}")]
        public IActionResult ObtenerTicketsPorUsuario(int usuarioId)
        {
            var resultado = tickets.Where(t => t.UsuarioId == usuarioId).ToList();
            return Ok(resultado);
        }

        // POST: api/tickets
        [HttpPost]
        public IActionResult CrearTicket([FromBody] Ticket ticket)
        {
            ticket.Id = tickets.Count + 1;
            ticket.Estado = "Abierto";
            ticket.FechaCreacion = DateTime.Now;

            tickets.Add(ticket);

            return Ok(ticket);
        }
    }
}