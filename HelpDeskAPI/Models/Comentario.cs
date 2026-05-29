using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace HelpDeskAPI.Models
{
    public class Comentario
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Mensaje { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public int TicketId { get; set; }

        [JsonIgnore]
        public Ticket? Ticket { get; set; }

        public int UsuarioId { get; set; }

        [JsonIgnore]
        public Usuario? Usuario { get; set; }
    }
}