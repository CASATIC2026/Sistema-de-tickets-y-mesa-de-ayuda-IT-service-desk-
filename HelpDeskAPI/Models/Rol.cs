namespace HelpDeskAPI.Models
{
    public class Rol
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;

        public List<UsuarioRol> UsuarioRoles { get; set; } = new();
    }
}
