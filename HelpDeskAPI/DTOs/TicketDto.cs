using System.ComponentModel.DataAnnotations;

namespace HelpDeskAPI.DTOs
{
    public class CrearTicketDto
    {
        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(200, MinimumLength = 5)]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(2000, MinimumLength = 10)]
        public string Descripcion { get; set; } = string.Empty;

        public string Prioridad { get; set; } = "Media";

        public int? CategoriaId { get; set; }
    }

    public class ActualizarTicketDto
    {
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public string? Prioridad { get; set; }
        public string? Estado { get; set; }
        public int? TecnicoAsignadoId { get; set; }
        public int? CategoriaId { get; set; }
    }

    public class AsignarTicketDto
    {
        [Required]
        public int TecnicoId { get; set; }
    }

    public class CambiarEstadoDto
    {
        [Required]
        [RegularExpression("^(Abierto|En Progreso|Resuelto|Cerrado)$")]
        public string Estado { get; set; } = string.Empty;
    }

    public class TicketResponseDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string? Prioridad { get; set; }
        public string? Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaLimite { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
        public int? TecnicoAsignadoId { get; set; }
        public string? TecnicoNombre { get; set; }
        public int? CategoriaId { get; set; }
        public string? CategoriaNombre { get; set; }
        public string? SlaStatus { get; set; }
    }
}