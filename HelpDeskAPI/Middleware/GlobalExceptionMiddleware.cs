using System.Net;
using System.Text.Json;

namespace HelpDeskAPI.Middleware
{
    /// <summary>
    /// Middleware global de excepciones.
    /// Captura cualquier excepción no manejada y devuelve un JSON estructurado
    /// sin exponer stack traces al cliente en producción.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex,
                    "Excepción no controlada. CorrelationId={CorrelationId} Path={Path}",
                    correlationId,
                    context.Request.Path);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var payload = new
                {
                    message = "Error interno del servidor",
                    correlationId,
                    detail = _env.IsDevelopment() ? ex.Message : null
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }
        }
    }
}
