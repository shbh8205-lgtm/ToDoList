using Microsoft.EntityFrameworkCore;
using TodoApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// --- 1. הגדרות שרת ופורט (Render) ---
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// --- 2. הגדרות בסיס נתונים (MySQL) ---
// שליפה מהקונפיגורציה - ב-Render הגדר משתנה בשם: ConnectionStrings__ToDoDB
var connectionString = builder.Configuration.GetConnectionString("ToDoDB");

builder.Services.AddDbContext<ToDoDbContext>(options => {
    if (!string.IsNullOrEmpty(connectionString))
    {
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 31));
        options.UseMySql(connectionString, serverVersion, mysqlOptions => 
        {
            mysqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
        });
    }
    else
    {
        // זריקת שגיאה ברורה כדי שלא נקבל "Login Error: Option name not supported" מבלבל
        throw new InvalidOperationException("[CRITICAL] Database Connection String 'ToDoDB' is missing!");
    }
});

// --- 3. אבטחה ו-JWT ---
var secretKey = builder.Configuration["Jwt:Key"] ?? "a_very_long_and_secure_default_key_for_dev_12345";
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
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
if (app.Environment.IsDevelopment() || true) // השארתי true כדי שתוכל לראות Swagger ב-Render לדיבאג
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// --- 6. Endpoints ---

app.MapGet("/", () => "API is Running!");

app.MapPost("/login", async (ToDoDbContext db, UserLogin loginData) =>
{
    try 
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
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Results.Ok(new { token = tokenHandler.WriteToken(token) });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Login Failed: {ex.Message}");
        return Results.Problem("Database connection error during login.");
    }
});

// --- Items Management ---

app.MapGet("/items", async (ToDoDbContext db, ClaimsPrincipal user) => 
{
    var userIdClaim = user.FindFirst("id")?.Value;
    if (userIdClaim == null) return Results.Unauthorized();

    int userId = int.Parse(userIdClaim);
    return Results.Ok(await db.Items.Where(t => t.UserId == userId).ToListAsync());
}).RequireAuthorization();

app.MapPost("/items", async (ToDoDbContext db, Item newItem, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirst("id")?.Value;
    if (userIdClaim == null) return Results.Unauthorized();

    newItem.UserId = int.Parse(userIdClaim);
    db.Items.Add(newItem);
    await db.SaveChangesAsync();
    return Results.Created($"/items/{newItem.Id}", newItem);
}).RequireAuthorization();

app.MapPut("/items/{id}", async (ToDoDbContext db, int id, Item updatedItem, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirst("id")?.Value;
    if (userIdClaim == null) return Results.Unauthorized();
    int userId = int.Parse(userIdClaim);

    var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
    if (item is null) return Results.NotFound();

    item.Name = updatedItem.Name;
    item.IsComplete = updatedItem.IsComplete;

    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/items/{id}", async (ToDoDbContext db, int id, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirst("id")?.Value;
    if (userIdClaim == null) return Results.Unauthorized();
    int userId = int.Parse(userIdClaim);

    var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
    if (item is null) return Results.NotFound();

    db.Items.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.Run();

// DTOs
public record UserLogin(string UserName, string Password);