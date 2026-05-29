using HelpDeskAPI.Models;
using HelpDeskAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskAPI.Data
{
    public class HelpDeskContext : DbContext
    {
        public HelpDeskContext(DbContextOptions<HelpDeskContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<UsuarioRol> UsuarioRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // RELACIÓN MUCHOS A MUCHOS
            modelBuilder.Entity<UsuarioRol>()
                .HasKey(ur => new { ur.UsuarioId, ur.RolId });

            modelBuilder.Entity<UsuarioRol>()
                .HasOne(ur => ur.Usuario)
                .WithMany(u => u.UsuarioRoles)
                .HasForeignKey(ur => ur.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UsuarioRol>()
                .HasOne(ur => ur.Rol)
                .WithMany(r => r.UsuarioRoles)
                .HasForeignKey(ur => ur.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            // ROLES BASE
            modelBuilder.Entity<Rol>().HasData(
                new Rol { Id = 1, Nombre = "Admin" },
                new Rol { Id = 2, Nombre = "Tecnico" },
                new Rol { Id = 3, Nombre = "Solicitante" }
            );

            // ÍNDICES E INTEGRIDAD
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Correo)
                .IsUnique();

            modelBuilder.Entity<Ticket>().HasIndex(t => t.Estado);
            modelBuilder.Entity<Ticket>().HasIndex(t => t.Prioridad);
            modelBuilder.Entity<Ticket>().HasIndex(t => t.FechaCreacion);
            modelBuilder.Entity<Comentario>().HasIndex(c => c.TicketId);
            modelBuilder.Entity<Comentario>().HasIndex(c => c.Fecha);

            modelBuilder.Entity<Categoria>()
                .HasIndex(c => c.Nombre)
                .IsUnique();

            // RELACIONES
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Usuario)
                .WithMany(u => u.TicketsCreados)
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.TecnicoAsignado)
                .WithMany(u => u.TicketsAsignados)
                .HasForeignKey(t => t.TecnicoAsignadoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Categoria)
                .WithMany(c => c.Tickets)
                .HasForeignKey(t => t.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Comentario>()
                .HasOne(c => c.Ticket)
                .WithMany(t => t.Comentarios)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comentario>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Comentarios)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // ======================================================
        // SEED (idempotente, lee credenciales desde IConfiguration)
        // ======================================================
        public async Task SeedDataAsync(IPasswordService passwordService, IConfiguration config)
        {
            if (!await Categorias.AnyAsync())
            {
                Categorias.AddRange(
                    new Categoria { Nombre = "Hardware", Descripcion = "Problemas con equipos físicos" },
                    new Categoria { Nombre = "Software", Descripcion = "Problemas con aplicaciones" },
                    new Categoria { Nombre = "Red", Descripcion = "Problemas de conectividad" },
                    new Categoria { Nombre = "Impresoras", Descripcion = "Problemas con impresoras" },
                    new Categoria { Nombre = "Correo", Descripcion = "Problemas con correo electrónico" },
                    new Categoria { Nombre = "Otro", Descripcion = "Otros problemas" }
                );
                await SaveChangesAsync();
            }

            var adminRol = await Roles.FirstAsync(r => r.Nombre == "Admin");
            var tecnicoRol = await Roles.FirstAsync(r => r.Nombre == "Tecnico");
            var userRol = await Roles.FirstAsync(r => r.Nombre == "Solicitante");

            await EnsureSeededUser(
                config["AdminSeed:Email"] ?? "admin@helpdesk.com",
                config["AdminSeed:Password"] ?? "Admin#Sec2026!Strong",
                config["AdminSeed:Nombre"] ?? "Administrador",
                adminRol, passwordService);

            await EnsureSeededUser(
                config["TecnicoSeed:Email"] ?? "tecnico@helpdesk.com",
                config["TecnicoSeed:Password"] ?? "Tecnico#Sec2026!Strong",
                config["TecnicoSeed:Nombre"] ?? "Tecnico Soporte",
                tecnicoRol, passwordService);

            await EnsureSeededUser(
                config["UsuarioSeed:Email"] ?? "usuario@helpdesk.com",
                config["UsuarioSeed:Password"] ?? "Usuario#Sec2026!Strong",
                config["UsuarioSeed:Nombre"] ?? "Usuario Demo",
                userRol, passwordService);
        }

        private async Task EnsureSeededUser(
            string email, string password, string nombre,
            Rol rol, IPasswordService passwordService)
        {
            var emailLower = email.Trim().ToLowerInvariant();
            var existing = await Usuarios
                .Include(u => u.UsuarioRoles)
                .FirstOrDefaultAsync(u => u.Correo == emailLower);

            if (existing == null)
            {
                var usuario = new Usuario
                {
                    Nombre = nombre,
                    Correo = emailLower,
                    Password = passwordService.HashPassword(password),
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };
                usuario.UsuarioRoles.Add(new UsuarioRol { Usuario = usuario, Rol = rol });
                Usuarios.Add(usuario);
                await SaveChangesAsync();
            }
            else if (!existing.UsuarioRoles.Any(ur => ur.RolId == rol.Id))
            {
                existing.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioId = existing.Id,
                    RolId = rol.Id
                });
                await SaveChangesAsync();
            }
        }
    }
}
