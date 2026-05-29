using System.ComponentModel.DataAnnotations;

namespace HelpDeskAPI.DTOs
{
    public class CrearComentarioDto
    {
        [Required]
        public int TicketId { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Mensaje { get; set; } = string.Empty;
    }

    public class ComentarioResponseDto
    {
        public int Id { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public int TicketId { get; set; }
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public string UsuarioRol { get; set; } = string.Empty;
    }
}