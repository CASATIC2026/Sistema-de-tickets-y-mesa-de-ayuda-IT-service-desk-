namespace HelpDeskAPI.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Password { get; set; }
        public string Rol { get; set; }

        public List<Ticket>? Tickets { get; set; }
        public List<Comentario>? Comentarios { get; set; }
    }
}