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

string CreateUserToken(User user)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role.ToString())
    };

    var token = new JwtSecurityToken(
        claims: claims,
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// ---------------------------------------------------------------------------
// POST /api/signup  [public]
// Creates a new user and returns a permanent JWT.
// ---------------------------------------------------------------------------
app.MapPost("/api/signup", async (LoginRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { error = "Username and password are required" });

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
    if (user is not null)
        return Results.Conflict(new { error = "Username already exists" });

    user = new User
    {
        Username = request.Username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new { token = CreateUserToken(user) });
});

// ---------------------------------------------------------------------------
// POST /api/login  [public]
// Validates an existing account and returns a permanent JWT.
// ---------------------------------------------------------------------------
app.MapPost("/api/login", async (LoginRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { error = "Username and password are required" });

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
    if (user is null)
        return Results.Unauthorized();

    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return Results.Unauthorized();

    return Results.Ok(new { token = CreateUserToken(user) });
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
// SPA fallback: if the path matches an actual file on disk, serve it with
// the correct MIME type. Otherwise return index.html for client-side routing.
// ---------------------------------------------------------------------------
var mimeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

app.MapGet("/fragments/{spaId}/{**path}", (string spaId, string? path) =>
{
    if (!string.IsNullOrEmpty(path))
    {
        var filePath = Path.Combine(fragmentsPath, spaId, path);
        if (File.Exists(filePath))
        {
            if (!mimeProvider.TryGetContentType(filePath, out var contentType))
                contentType = "application/octet-stream";
            return Results.File(filePath, contentType);
        }
    }

    var indexPath = Path.Combine(fragmentsPath, spaId, "index.html");
    return File.Exists(indexPath)
        ? Results.File(indexPath, "text/html")
        : Results.NotFound();
});

// ---------------------------------------------------------------------------
// GET /api/status  [public]
// ---------------------------------------------------------------------------
app.MapGet("/api/status", async (AppDbContext db) =>
{
    var version = await db.Meta.FirstOrDefaultAsync(m => m.Key == "version");

    return Results.Ok(new
    {
        version = version?.Value ?? "unknown",
        updatedAt = version?.UpdatedAt
    });
});

// ---------------------------------------------------------------------------
// POST /api/management/version  [admin]
// ---------------------------------------------------------------------------
app.MapPost("/api/management/version", async (VersionRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Version))
        return Results.BadRequest(new { error = "Version is required" });

    var meta = await db.Meta.FirstOrDefaultAsync(m => m.Key == "version");

    if (meta is null)
    {
        meta = new Meta { Key = "version", Value = request.Version };
        db.Meta.Add(meta);
    }
    else
    {
        meta.Value = request.Version;
        meta.UpdatedAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { version = meta.Value, updatedAt = meta.UpdatedAt });
}).RequireAuthorization().AddEndpointFilter(AdminAuthFilter);

// ---------------------------------------------------------------------------
// GET /api/management/users  [admin]
// ---------------------------------------------------------------------------
app.MapGet("/api/management/users", async (AppDbContext db) =>
{
    var users = await db.Users
        .Select(u => new { u.Id, u.Username, Role = u.Role.ToString(), u.CreatedAt })
        .ToListAsync();

    return Results.Ok(users);
}).RequireAuthorization().AddEndpointFilter(AdminAuthFilter);

// ---------------------------------------------------------------------------
// PUT /api/management/server  [admin]
// ---------------------------------------------------------------------------
app.MapPut("/api/management/server", async (ServerRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Address) || request.Port <= 0)
        return Results.BadRequest(new { error = "Name, address, and a valid port are required" });

    var server = new GameServer
    {
        Name = request.Name,
        Address = request.Address,
        Port = request.Port,
        IsActive = true
    };
    db.GameServers.Add(server);
    await db.SaveChangesAsync();

    return Results.Ok(new { server.Id, server.Name, server.Address, server.Port, server.IsActive });
}).RequireAuthorization().AddEndpointFilter(AdminAuthFilter);

// ---------------------------------------------------------------------------
// GET /api/management/servers  [admin]
// ---------------------------------------------------------------------------
app.MapGet("/api/management/servers", async (AppDbContext db) =>
{
    var servers = await db.GameServers
        .Select(s => new { s.Id, s.Name, s.Address, s.Port, s.IsActive, s.CreatedAt })
        .ToListAsync();

    return Results.Ok(servers);
}).RequireAuthorization().AddEndpointFilter(AdminAuthFilter);

// ---------------------------------------------------------------------------
// PUT /api/management/user/{id}/role  [admin]
// ---------------------------------------------------------------------------
app.MapPut("/api/management/user/{id}/role", async (int id, RoleRequest request, AppDbContext db) =>
{
    if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        return Results.BadRequest(new { error = "Invalid role. Must be one of: Player, Mod, Admin" });

    var user = await db.Users.FindAsync(id);
    if (user is null)
        return Results.NotFound(new { error = "User not found" });

    user.Role = role;
    await db.SaveChangesAsync();

    return Results.Ok(new { user.Id, user.Username, Role = user.Role.ToString() });
}).RequireAuthorization().AddEndpointFilter(AdminAuthFilter);

// ---------------------------------------------------------------------------
// DELETE /api/management/server/{id}  [admin]
// ---------------------------------------------------------------------------
app.MapDelete("/api/management/server/{id}", async (int id, AppDbContext db) =>
{
    var server = await db.GameServers.FindAsync(id);
    if (server is null)
        return Results.NotFound(new { error = "Server not found" });

    db.GameServers.Remove(server);
    await db.SaveChangesAsync();

    return Results.Ok(new { deleted = id });
}).RequireAuthorization().AddEndpointFilter(AdminAuthFilter);

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

// ---------------------------------------------------------------------------
// Admin auth endpoint filter — checks JWT role claim for Admin
// ---------------------------------------------------------------------------
static async ValueTask<object?> AdminAuthFilter(
    EndpointFilterInvocationContext context,
    EndpointFilterDelegate next)
{
    var role = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);

    if (role != nameof(UserRole.Admin))
        return Results.Json(new { error = "Admin access required" }, statusCode: 403);

    return await next(context);
}

record LoginRequest(string Username, string Password);
record ServerRequest(string Name, string Address, int Port);
record VersionRequest(string Version);
record RoleRequest(string Role);
