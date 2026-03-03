namespace HelpDeskAPI.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string Prioridad { get; set; }
        public string Estado { get; set; }
        public DateTime FechaCreacion { get; set; }

        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }

        public List<Comentario>? Comentarios { get; set; }
    }
}