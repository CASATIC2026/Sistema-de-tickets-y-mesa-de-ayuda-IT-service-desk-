using System.ComponentModel.DataAnnotations;

namespace HelpDeskAPI.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public bool Activa { get; set; } = true;

        public List<Ticket>? Tickets { get; set; }
    }
}