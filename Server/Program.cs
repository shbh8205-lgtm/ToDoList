using Microsoft.EntityFrameworkCore;
using TodoApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var secretKey = builder.Configuration["Jwt:Key"];

// אם המפתח חסר, נזרוק שגיאה ברורה
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT Secret Key is missing from configuration.");
}

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

var connectionString = builder.Configuration.GetConnectionString("ToDoDB");
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapGet("/items", async (ToDoDbContext db, ClaimsPrincipal user) => 
{
    // חילוץ ה-ID מהטוקן (הגדרנו אותו תחת השם "id" ב-Login)
    var userIdClaim = user.FindFirst("id")?.Value;
    
    if (userIdClaim == null) return Results.Unauthorized();

    int userId = int.Parse(userIdClaim);

    // סינון המשימות לפי ה-ID של המשתמש
    var userTasks = await db.Items
                            .Where(todo => todo.UserId == userId)
                            .ToListAsync();
                            
    return Results.Ok(userTasks);
}).RequireAuthorization();

app.MapPost("/items", async (ToDoDbContext db, Item newItem, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirst("id")?.Value;
    if (userIdClaim == null) return Results.Unauthorized();

    // הצמדת המשימה למשתמש הנוכחי
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

    // מחפשים משימה שה-ID שלה תואם ושייכת למשתמש המחובר
    var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

    if (item is null) return Results.NotFound("המשימה לא נמצאה או שאינה שייכת לך");

    // עדכון השדות
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

    // מנסים למצוא את המשימה הספציפית של המשתמש הספציפי
    var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

    if (item is null) return Results.NotFound("לא ניתן למחוק משימה שלא קיימת או שאינה שלך");

    db.Items.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapPost("/login", async (ToDoDbContext db, UserLogin loginData) =>
{
    var foundUser = await db.Users
        .FirstOrDefaultAsync(u => u.Name == loginData.UserName && u.Password == loginData.Password);

    if (foundUser is null) return Results.Unauthorized();

    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new System.Security.Claims.ClaimsIdentity(new[] 
        { 
            new System.Security.Claims.Claim("id", foundUser.Id.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, foundUser.Name)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new { token = tokenHandler.WriteToken(token) });
});

app.MapGet("/", () => "Hello World!");
app.Run();

public record UserLogin(string UserName, string Password);