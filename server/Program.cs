using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using PiratesQuest.Server.Data;
using PiratesQuest.Server.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseDefaultFiles();
app.UseStaticFiles();

var fragmentsPath = Path.Combine(app.Environment.ContentRootPath, "fragments");
if (!Directory.Exists(fragmentsPath))
    Directory.CreateDirectory(fragmentsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(fragmentsPath),
    RequestPath = "/fragments"
});

app.UseAuthentication();
app.UseAuthorization();

var serverApiKey = builder.Configuration["ServerApiKey"]
    ?? throw new InvalidOperationException("ServerApiKey is not configured");

// ---------------------------------------------------------------------------
// POST /api/login  [public]
// Creates a new user if username doesn't exist, otherwise validates password.
// Returns a permanent JWT.
// ---------------------------------------------------------------------------
app.MapPost("/api/login", async (LoginRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { error = "Username and password are required" });

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

    if (user is null)
    {
        user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
    else
    {
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Results.Unauthorized();
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };

    var token = new JwtSecurityToken(
        claims: claims,
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
    );

    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
});

// ---------------------------------------------------------------------------
// GET /api/servers  [authenticated]
// ---------------------------------------------------------------------------
app.MapGet("/api/servers", async (AppDbContext db) =>
{
    var servers = await db.GameServers
        .Where(s => s.IsActive)
        .Select(s => new { s.Id, s.Name, s.Address, s.Port })
        .ToListAsync();

    return Results.Ok(servers);
}).RequireAuthorization();

// ---------------------------------------------------------------------------
// GET /api/server/{id}/state/{user}  [server auth]
// ---------------------------------------------------------------------------
app.MapGet("/api/server/{id}/state/{user}", async (int id, string user, AppDbContext db) =>
{
    var state = await db.GameStates
        .FirstOrDefaultAsync(s => s.ServerId == id && s.UserId == user);

    return state is null
        ? Results.NotFound()
        : Results.Content(state.State, "application/json");
}).AddEndpointFilter(ServerAuthFilter);

// ---------------------------------------------------------------------------
// PUT /api/server/{id}/state/{user}  [server auth]
// ---------------------------------------------------------------------------
app.MapPut("/api/server/{id}/state/{user}", async (int id, string user, HttpContext context, AppDbContext db) =>
{
    if (!await db.GameServers.AnyAsync(s => s.Id == id))
        return Results.NotFound(new { error = "Server not found" });

    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(body))
        return Results.BadRequest(new { error = "Request body is required" });

    var state = await db.GameStates
        .FirstOrDefaultAsync(s => s.ServerId == id && s.UserId == user);

    if (state is null)
    {
        state = new GameState { ServerId = id, UserId = user, State = body };
        db.GameStates.Add(state);
    }
    else
    {
        state.State = body;
        state.UpdatedAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();
    return Results.Ok();
}).AddEndpointFilter(ServerAuthFilter);

// ---------------------------------------------------------------------------
// GET /fragments/{spaId}/{**path}  [public]
// Serves the SPA's index.html, falling back for client-side routing.
// Static assets (js/css) are handled by the static files middleware above.
// ---------------------------------------------------------------------------
app.MapGet("/fragments/{spaId}/{**path}", (string spaId) =>
{
    var indexPath = Path.Combine(fragmentsPath, spaId, "index.html");
    return File.Exists(indexPath)
        ? Results.File(indexPath, "text/html")
        : Results.NotFound();
});

app.Run();

// ---------------------------------------------------------------------------
// Server auth endpoint filter — validates X-Server-Key header
// ---------------------------------------------------------------------------
static async ValueTask<object?> ServerAuthFilter(
    EndpointFilterInvocationContext context,
    EndpointFilterDelegate next)
{
    var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
    var expected = config["ServerApiKey"];
    var provided = context.HttpContext.Request.Headers["X-Server-Key"].FirstOrDefault();

    if (string.IsNullOrEmpty(provided) || provided != expected)
        return Results.Unauthorized();

    return await next(context);
}

record LoginRequest(string Username, string Password);
