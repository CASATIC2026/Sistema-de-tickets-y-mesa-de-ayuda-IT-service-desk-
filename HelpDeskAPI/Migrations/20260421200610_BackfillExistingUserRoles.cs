using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskAPI.Migrations
{
    /// <inheritdoc />
    public partial class BackfillExistingUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""UsuarioRoles"" (""UsuarioId"", ""RolId"")
                SELECT 
                    u.""Id"",
                    CASE
                        WHEN lower(u.""Correo"") = 'admin@helpdesk.com' THEN 1
                        WHEN lower(u.""Correo"") = 'tecnico@helpdesk.com' THEN 2
                        ELSE 3
                    END
                FROM ""Usuarios"" u
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ""UsuarioRoles"" ur
                    WHERE ur.""UsuarioId"" = u.""Id""
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""UsuarioRoles""
                WHERE ""UsuarioId"" IN (
                    SELECT u.""Id""
                    FROM ""Usuarios"" u
                    WHERE lower(u.""Correo"") = 'admin@helpdesk.com'
                       OR lower(u.""Correo"") = 'tecnico@helpdesk.com'
                       OR lower(u.""Correo"") = 'usuario@helpdesk.com'
                );
            ");
        }
    }
}
