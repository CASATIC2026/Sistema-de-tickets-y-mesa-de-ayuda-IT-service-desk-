using HelpDeskAPI.Data;
using HelpDeskAPI.DTOs;
using HelpDeskAPI.Models;
using HelpDeskAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HelpDeskAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly HelpDeskContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            HelpDeskContext context,
            IPasswordService passwordService,
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
            _logger = logger;
        }

        // ======================================================
        // LOGIN  (rate-limited contra brute-force)
        // ======================================================
        [HttpPost("login")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var correoNormalizado = login.Correo.Trim().ToLowerInvariant();

            var usuario = await _context.Usuarios
                .Include(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(u => u.Correo.ToLower() == correoNormalizado && u.Activo);

            // Mensaje genérico para evitar enumeración de usuarios
            const string credencialesInvalidas = "Credenciales inválidas";

            if (usuario == null)
            {
                _logger.LogWarning("Login fallido: usuario {Correo} no encontrado o inactivo", correoNormalizado);
                return Unauthorized(new { message = credencialesInvalidas });
            }

            if (!_passwordService.VerifyPassword(login.Password, usuario.Password))
            {
                _logger.LogWarning("Login fallido: password inválido para {Correo}", correoNormalizado);
                return Unauthorized(new { message = credencialesInvalidas });
            }

            // Rehash automático si es necesario
            if (_passwordService.NeedsRehash(usuario.Password))
            {
                usuario.Password = _passwordService.HashPassword(login.Password);
                await _context.SaveChangesAsync();
            }

            // Reparar rol si no existe
            if (!usuario.UsuarioRoles.Any())
            {
                await RepararRolSiHaceFalta(usuario);
                await _context.Entry(usuario)
                    .Collection(u => u.UsuarioRoles)
                    .Query()
                    .Include(ur => ur.Rol)
                    .LoadAsync();
            }

            var rol = _jwtService.ObtenerRolPrioritario(usuario);
            var token = _jwtService.GenerateToken(usuario, login.TokenExpiration);

            _logger.LogInformation("Login OK usuario={UserId} rol={Rol}", usuario.Id, rol);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddHours(login.TokenExpiration),
                Usuario = new UsuarioResponseDto
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre ?? "",
                    Correo = usuario.Correo,
                    Rol = rol
                }
            });
        }

        // ======================================================
        // REGISTER  (siempre crea como Solicitante; ignora cualquier rol enviado)
        // ======================================================
        [HttpPost("register")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registro)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var correoNormalizado = registro.Correo.Trim().ToLowerInvariant();

            var existe = await _context.Usuarios
                .AnyAsync(u => u.Correo.ToLower() == correoNormalizado);

            if (existe)
                return BadRequest(new { message = "El correo ya está registrado" });

            var rolSolicitante = await _context.Roles
                .FirstOrDefaultAsync(r => r.Nombre == "Solicitante");

            if (rolSolicitante == null)
                return StatusCode(500, new { message = "Configuración de roles inválida en el servidor" });

            var usuario = new Usuario
            {
                Nombre = registro.Nombre.Trim(),
                Correo = correoNormalizado,
                Password = _passwordService.HashPassword(registro.Password),
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            usuario.UsuarioRoles.Add(new UsuarioRol
            {
                Usuario = usuario,
                Rol = rolSolicitante
            });

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            await _context.Entry(usuario)
                .Collection(u => u.UsuarioRoles)
                .Query()
                .Include(ur => ur.Rol)
                .LoadAsync();

            var rol = _jwtService.ObtenerRolPrioritario(usuario);
            var token = _jwtService.GenerateToken(usuario, 2);

            _logger.LogInformation("Registro nuevo usuario={UserId} correo={Correo}", usuario.Id, correoNormalizado);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddHours(2),
                Usuario = new UsuarioResponseDto
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre ?? "",
                    Correo = usuario.Correo,
                    Rol = rol
                }
            });
        }

        // ======================================================
        // GET CURRENT USER
        // ======================================================
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = "Token inválido" });

            var usuario = await _context.Usuarios
                .Include(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(u => u.Id == userId && u.Activo);

            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado" });

            return Ok(new UsuarioResponseDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre ?? "",
                Correo = usuario.Correo,
                Rol = _jwtService.ObtenerRolPrioritario(usuario)
            });
        }

        // ======================================================
        // CAMBIAR CONTRASEÑA
        // ======================================================
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = "Token inválido" });

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId && u.Activo);
            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado" });

            if (!_passwordService.VerifyPassword(dto.PasswordActual, usuario.Password))
                return BadRequest(new { message = "Contraseña actual incorrecta" });

            usuario.Password = _passwordService.HashPassword(dto.PasswordNueva);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cambio de contraseña usuario={UserId}", userId);
            return Ok(new { message = "Contraseña actualizada" });
        }

        // ======================================================
        // GET USUARIOS  (solo Admin)
        // ======================================================
        [HttpGet("usuarios")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuariosDb = await _context.Usuarios
                .Include(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.Rol)
                .Where(u => u.Activo)
                .ToListAsync();

            var usuarios = usuariosDb.Select(u => new UsuarioResponseDto
            {
                Id = u.Id,
                Nombre = u.Nombre ?? "",
                Correo = u.Correo,
                Rol = _jwtService.ObtenerRolPrioritario(u)
            }).ToList();

            return Ok(usuarios);
        }

        // ======================================================
        // GET TECNICOS
        // ======================================================
        [HttpGet("tecnicos")]
        [Authorize(Roles = "Admin,Tecnico")]
        public async Task<IActionResult> GetTecnicos()
        {
            var tecnicosDb = await _context.Usuarios
                .Include(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.Rol)
                .Where(u => u.Activo &&
                    u.UsuarioRoles.Any(ur => ur.Rol.Nombre == "Tecnico" || ur.Rol.Nombre == "Admin"))
                .ToListAsync();

            var tecnicos = tecnicosDb.Select(u => new UsuarioResponseDto
            {
                Id = u.Id,
                Nombre = u.Nombre ?? "",
                Correo = u.Correo,
                Rol = _jwtService.ObtenerRolPrioritario(u)
            }).ToList();

            return Ok(tecnicos);
        }

        // ======================================================
        // DESACTIVAR USUARIO
        // ======================================================
        [HttpDelete("usuarios/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int currentUserId) && currentUserId == id)
                return BadRequest(new { message = "No puedes desactivarte a ti mismo" });

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado" });

            usuario.Activo = false;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Usuario desactivado id={Id}", id);
            return Ok(new { message = "Usuario desactivado correctamente" });
        }

        // ======================================================
        // REPARAR ROL SI USUARIO NO TIENE
        // ======================================================
        private async Task RepararRolSiHaceFalta(Usuario usuario)
        {
            if (usuario.UsuarioRoles.Any()) return;

            var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Solicitante");
            if (rol == null) return;

            usuario.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioId = usuario.Id,
                RolId = rol.Id
            });
            await _context.SaveChangesAsync();
        }
    }
}
