using Microsoft.EntityFrameworkCore;
using TodoApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// 1. הגדרת הפורט עבור Render - קריטי למניעת שגיאות Kestrel
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseKestrel().ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// 2. שליפת סוד ה-JWT (ודא שזה מוגדר ב-Environment Variables ב-Render)
var secretKey = builder.Configuration["Jwt:Key"] ?? "a_very_long_and_secure_default_key_for_dev";
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
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();




// 1. שליפת מחרוזת החיבור מהקונפיגורציה (מה שמוגדר ב-Render)
var rawConnectionString = builder.Configuration.GetConnectionString("ToDoDB");

// 2. הדפסת דיבאג ללוג - זה יגיד לנו מה Render באמת שולח
if (!string.IsNullOrEmpty(rawConnectionString))
{
    var censored = System.Text.RegularExpressions.Regex.Replace(rawConnectionString, @"password=.*?;", "password=***;", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    Console.WriteLine($"[DEBUG] Connection String in use: {censored}");
}

// 3. הגדרת ה-DbContext עם גרסה ידנית לעקיפת השגיאה
builder.Services.AddDbContext<ToDoDbContext>(options => {
    if (!string.IsNullOrEmpty(rawConnectionString))
    {
        // הגדרת גרסה 8.0.31 (מתאים לרוב שירותי הענן כמו Clever Cloud)
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 31));
        
        options.UseMySql(rawConnectionString, serverVersion, mysqlOptions => 
        {
            mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
    }
});







// 4. CORS - פתוח לכולם (מתאים ל-OPTIONS 204 שראינו)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// הצגת דף שגיאות מפורט גם בענן כדי שתוכל לראות מה ה-500 (רק לצורך דיבאג!)
app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// --- Endpoints ---

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
        // זה ידפיס ללוג של Render את השגיאה האמיתית
        Console.WriteLine($"Login Error: {ex.Message}");
        return Results.Problem("Internal Server Error during login");
    }
});

app.MapGet("/items", async (ToDoDbContext db, ClaimsPrincipal user) => 
{
    var userIdClaim = user.FindFirst("id")?.Value;
    if (userIdClaim == null) return Results.Unauthorized();

    int userId = int.Parse(userIdClaim);
    var userTasks = await db.Items.Where(todo => todo.UserId == userId).ToListAsync();
    return Results.Ok(userTasks);
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

public record UserLogin(string UserName, string Password);