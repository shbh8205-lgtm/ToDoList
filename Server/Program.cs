using Microsoft.EntityFrameworkCore;
using TodoApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// --- 1. הגדרות שרת (Render) ---
// Render מעבירה את הפורט במשתנה סביבה. אם הוא לא קיים, נשתמש ב-8080 כברירת מחדל מקומית.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// --- 2. הגדרות בסיס נתונים (MySQL) ---
var connectionString = builder.Configuration.GetConnectionString("ToDoDB") 
                       ?? builder.Configuration["ConnectionStrings__ToDoDB"];


builder.Services.AddDbContext<ToDoDbContext>(options => {
    if (!string.IsNullOrEmpty(connectionString))
    {
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 31));
        options.UseMySql(connectionString.Trim(), serverVersion, mysqlOptions => {
            mysqlOptions.EnableRetryOnFailure();
        });
    }
});

// --- 3. אבטחה ו-JWT ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "a_very_long_and_secure_default_key_for_dev_123456789";
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// --- 4. CORS וכלים נוספים ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- 5. Middleware Pipeline ---
// מאפשר Swagger גם ב-Production לצרכי דיבאג ב-Render
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// --- 6. Helpers ---
static int? GetUserId(ClaimsPrincipal user)
{
    var claim = user.FindFirst("id")?.Value;
    return int.TryParse(claim, out var id) ? id : null;
}

// --- 7. Endpoints ---

app.MapGet("/", () => "API is Running!").AllowAnonymous();

app.MapPost("/login", async (ToDoDbContext db, UserLogin loginData) =>
{
    var foundUser = await db.Users
        .FirstOrDefaultAsync(u => u.Name == loginData.UserName && u.Password == loginData.Password);

    if (foundUser is null) return Results.Unauthorized();

    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[] 
        { 
            new Claim("id", foundUser.Id.ToString()),
            new Claim(ClaimTypes.Name, foundUser.Name)
        }),
        Expires = DateTime.UtcNow.AddDays(7),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new { token = tokenHandler.WriteToken(token) });
});

// --- Items Management ---

app.MapGet("/items", async (ToDoDbContext db, ClaimsPrincipal user) => 
{
    var userId = GetUserId(user);
    if (userId == null) return Results.Unauthorized();

    var items = await db.Items.Where(t => t.UserId == userId).ToListAsync();
    return Results.Ok(items);
}).RequireAuthorization();

app.MapPost("/items", async (ToDoDbContext db, Item newItem, ClaimsPrincipal user) =>
{
    var userId = GetUserId(user);
    if (userId == null) return Results.Unauthorized();

    newItem.UserId = userId.Value;
    db.Items.Add(newItem);
    await db.SaveChangesAsync();
    return Results.Created($"/items/{newItem.Id}", newItem);
}).RequireAuthorization();

app.MapPut("/items/{id}", async (ToDoDbContext db, int id, Item updatedItem, ClaimsPrincipal user) =>
{
    var userId = GetUserId(user);
    if (userId == null) return Results.Unauthorized();

    var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
    if (item is null) return Results.NotFound();

    item.TaskName = updatedItem.TaskName;
    item.IsComplete = updatedItem.IsComplete;

    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/items/{id}", async (ToDoDbContext db, int id, ClaimsPrincipal user) =>
{
    var userId = GetUserId(user);
    if (userId == null) return Results.Unauthorized();

    var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
    if (item is null) return Results.NotFound();

    db.Items.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.Run();

// DTOs
public record UserLogin(string UserName, string Password);