using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoApi;

[Table("users")] // מגדיר ל-Entity Framework שהטבלה ב-MySQL נקראת users (בקטנות)
public class User
{
    [Key] // מגדיר שזה ה-Primary Key
    [Column("id")] // מקשר לעמודה id ב-MySQL
    public int Id { get; set; }

    [Column("name")] // מקשר לעמודה name ב-MySQL
    public string Name { get; set; } = null!;

    [Column("password")] // מקשר לעמודה password ב-MySQL
    public string Password { get; set; } = null!;

    // רשימת המשימות של המשתמש
    public List<Item> Items { get; set; } = new();
}