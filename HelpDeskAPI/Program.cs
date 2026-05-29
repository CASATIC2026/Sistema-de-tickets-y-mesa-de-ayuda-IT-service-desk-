using HelpDeskAPI.Data;
using HelpDeskAPI.Middleware;
using HelpDeskAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// CONFIGURACIÓN: permite override por variables de entorno
// ======================================================
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ======================================================
// CORS ESTRICTO POR CONFIGURACIÓN (Orígenes permitidos)
// ======================================================
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = allowedOrigins.ToList();

        policy.SetIsOriginAllowed(origin =>
              origins.Contains(origin, StringComparer.OrdinalIgnoreCase) ||
              IsAllowedLanOrigin(origin))
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

static bool IsAllowedLanOrigin(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        return false;

    if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        return false;

    if (uri.Host.Equals("local" + "host", StringComparison.OrdinalIgnoreCase))
        return false;

    if (!IPAddress.TryParse(uri.Host, out var ipAddress))
        return true;

    if (IPAddress.IsLoopback(ipAddress))
        return false;

    if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        return ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal;

    var bytes = ipAddress.GetAddressBytes();
    return bytes[0] == 10 ||
           (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
           (bytes[0] == 192 && bytes[1] == 168);
}

// ======================================================
// JSON OPTIONS
// ======================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ======================================================
// DATABASE
// ======================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no configurada.");

builder.Services.AddDbContext<HelpDeskContext>(options =>
    options.UseNpgsql(connectionString));

// ======================================================
// SERVICIOS DE APLICACIÓN
// ======================================================
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddHttpContextAccessor();

// ======================================================
// JWT AUTH (validación robusta de longitud de clave)
// ======================================================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT:Key no configurada");

if (Encoding.UTF8.GetBytes(jwtKey).Length < 32)
    throw new InvalidOperationException("JWT:Key debe tener al menos 32 bytes (256 bits) para HMAC-SHA256.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HelpDeskAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HelpDeskApp";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("TecnicoOAdmin", p => p.RequireRole("Admin", "Tecnico"));
    options.AddPolicy("AuthenticatedUser", p => p.RequireAuthenticatedUser());
});

// ======================================================
// RATE LIMITING (anti brute-force en login)
// ======================================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (context.Request.Method == "OPTIONS")
        {
            return RateLimitPartition.GetNoLimiter("NoLimitForPreflight");
        }
        return RateLimitPartition.GetNoLimiter("Default");
    });

    options.AddFixedWindowLimiter("GlobalPolicy", o =>
    {
        o.PermitLimit = 200;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("LoginPolicy", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
});

// ======================================================
// SWAGGER
// ======================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HelpDesk API",
        Version = "v1",
        Description = "API para sistema de Mesa de Ayuda con JWT y roles"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Escribe: Bearer {tu_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ======================================================
// CONSTRUCCIÓN DE LA APLICACIÓN (¡Solo una vez y aquí!)
// ======================================================
var app = builder.Build();


// ======================================================
// MIGRACIÓN + SEED
// ======================================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HelpDeskContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Ejecutamos las tareas asíncronas de manera segura en el inicio del pipeline
        await context.Database.MigrateAsync().ConfigureAwait(false);
        await context.SeedDataAsync(passwordService, builder.Configuration).ConfigureAwait(false);
        logger.LogInformation("Base de datos migrada y seed completado.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Error fatal durante migración / seed. La aplicación se detendrá.");
        throw;
    }
}

// ======================================================
// PIPELINE (MIDDLEWARES)
// ======================================================
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// 1. ESTO YA LO TIENES: Habilita el CORS seguro que analiza tu red local
app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Asegúrate de que el mapeo de controladores quede abajo de UseCors y UseAuthorization
app.MapControllers().RequireRateLimiting("GlobalPolicy");

app.Run();