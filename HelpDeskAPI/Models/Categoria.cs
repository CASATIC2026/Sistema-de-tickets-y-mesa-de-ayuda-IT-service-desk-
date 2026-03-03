namespace HelpDeskAPI.Models
{
    public class Categoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        public List<Ticket>? Tickets { get; set; }
    }
}