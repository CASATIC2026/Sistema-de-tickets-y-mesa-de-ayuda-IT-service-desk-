using HelpDeskAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HelpDeskAPI.Services
{
    public interface IJwtService
    {
        string GenerateToken(Usuario usuario, int expirationHours = 2);
        string ObtenerRolPrioritario(Usuario usuario);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // ======================================================
        // GENERAR TOKEN JWT SEGURO
        // ======================================================
        public string GenerateToken(Usuario usuario, int expirationHours = 2)
        {
            // Validación segura de duración (rango 1..24)
            if (expirationHours < 1 || expirationHours > 24)
                expirationHours = int.TryParse(_configuration["Jwt:ExpirationHours"], out var def) ? def : 2;

            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key no configurada");

            var keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length < 32)
                throw new InvalidOperationException("JWT Key insegura: menor a 256 bits.");

            var issuer = _configuration["Jwt:Issuer"] ?? "HelpDeskAPI";
            var audience = _configuration["Jwt:Audience"] ?? "HelpDeskApp";

            var securityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var rolPrioritario = ObtenerRolPrioritario(usuario);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Correo ?? string.Empty),
                new Claim(ClaimTypes.Name, usuario.Nombre ?? string.Empty),
                new Claim(ClaimTypes.Role, rolPrioritario),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ======================================================
        // PRIORIDAD CORRECTA DE ROLES
        // ======================================================
        public string ObtenerRolPrioritario(Usuario usuario)
        {
            if (usuario.UsuarioRoles == null || !usuario.UsuarioRoles.Any())
                return "Solicitante";

            var roles = usuario.UsuarioRoles
                .Where(ur => ur.Rol != null && !string.IsNullOrWhiteSpace(ur.Rol.Nombre))
                .Select(ur => ur.Rol.Nombre.Trim())
                .ToList();

            if (roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                return "Admin";

            if (roles.Any(r => r.Equals("Tecnico", StringComparison.OrdinalIgnoreCase)))
                return "Tecnico";

            return "Solicitante";
        }
    }
}
