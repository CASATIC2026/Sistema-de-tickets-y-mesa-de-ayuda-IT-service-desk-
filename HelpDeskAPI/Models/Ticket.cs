using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpDeskAPI.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Descripcion { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Prioridad { get; set; } = "Media";

        [StringLength(20)]
        public string? Estado { get; set; } = "Abierto";

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaLimite { get; set; }

        public DateTime? FechaResolucion { get; set; }

        public int? UsuarioId { get; set; }

        [JsonIgnore]
        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        public int? TecnicoAsignadoId { get; set; }

        [JsonIgnore]
        [ForeignKey("TecnicoAsignadoId")]
        public Usuario? TecnicoAsignado { get; set; }

        public int? CategoriaId { get; set; }

        [JsonIgnore]
        public Categoria? Categoria { get; set; }

        [JsonIgnore]
        public List<Comentario>? Comentarios { get; set; }

        [NotMapped]
        public string SlaStatus
        {
            get
            {
                if (Estado == "Resuelto" || Estado == "Cerrado")
                    return FechaResolucion <= FechaLimite ? "Cumplido" : "Vencido";

                if (FechaLimite == null)
                    return "Sin SLA";

                var now = DateTime.UtcNow;
                if (now > FechaLimite)
                    return "Vencido";

                var timeRemaining = FechaLimite.Value - now;
                if (timeRemaining.TotalMinutes <= 30)
                    return "Por vencer";

                return "En tiempo";
            }
        }
    }
}