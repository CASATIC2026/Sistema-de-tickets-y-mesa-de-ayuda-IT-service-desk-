using Microsoft.AspNetCore.Mvc;
using HelpDeskAPI.Models;

namespace HelpDeskAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] Usuario usuario)
        {
            if (usuario.Correo == "test@correo.com" && usuario.Password == "1234")
            {
                return Ok(new
                {
                    id = 1,
                    rol = "Solicitante" // 👈 mayúscula para que coincida con tu JS
                });
            }

            if (usuario.Correo == "tecnico@correo.com" && usuario.Password == "5678")
            {
                return Ok(new
                {
                    id = 2,
                    rol = "Tecnico"
                });
            }
            return Unauthorized("Credenciales incorrectas");
        }
    }
}