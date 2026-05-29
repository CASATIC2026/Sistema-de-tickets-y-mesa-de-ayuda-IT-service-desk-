using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace HelpDeskAPI.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string? Nombre { get; set; }

        [Required]
        [EmailAddress]
        public string Correo { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [StringLength(20)]
        public List<UsuarioRol> UsuarioRoles { get; set; } = new();

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public bool Activo { get; set; } = true;

        [JsonIgnore]
        public List<Ticket>? TicketsCreados { get; set; }

        [JsonIgnore]
        public List<Ticket>? TicketsAsignados { get; set; }

        [JsonIgnore]
        public List<Comentario>? Comentarios { get; set; }
    }
}