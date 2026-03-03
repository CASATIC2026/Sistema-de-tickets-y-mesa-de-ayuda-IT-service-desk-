namespace HelpDeskAPI.Models
{
    public class Comentario
    {
        public int Id { get; set; }
        public string Mensaje { get; set; }
        public DateTime Fecha { get; set; }

        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
    }
}