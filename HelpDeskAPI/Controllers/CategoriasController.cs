using HelpDeskAPI.Data;
using HelpDeskAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]   // <- corrige vulnerabilidad: ya no es público
    public class CategoriasController : ControllerBase
    {
        private readonly HelpDeskContext _context;

        public CategoriasController(HelpDeskContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodas()
        {
            var categorias = await _context.Categorias
                .Where(c => c.Activa)
                .OrderBy(c => c.Nombre)
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.Descripcion,
                    TotalTickets = c.Tickets != null ? c.Tickets.Count : 0
                })
                .ToListAsync();

            return Ok(categorias);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Crear([FromBody] Categoria categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria.Nombre))
                return BadRequest(new { message = "El nombre es obligatorio" });

            categoria.Nombre = categoria.Nombre.Trim();

            if (await _context.Categorias.AnyAsync(c => c.Nombre.ToLower() == categoria.Nombre.ToLower()))
                return BadRequest(new { message = "Ya existe una categoría con ese nombre" });

            categoria.Activa = true;
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            return Ok(categoria);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] Categoria categoriaActualizada)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
                return NotFound(new { message = "Categoría no encontrada" });

            if (!string.IsNullOrWhiteSpace(categoriaActualizada.Nombre))
                categoria.Nombre = categoriaActualizada.Nombre.Trim();

            if (categoriaActualizada.Descripcion != null)
                categoria.Descripcion = categoriaActualizada.Descripcion;

            await _context.SaveChangesAsync();
            return Ok(categoria);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Desactivar(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
                return NotFound(new { message = "Categoría no encontrada" });

            categoria.Activa = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Categoría desactivada" });
        }
    }
}
