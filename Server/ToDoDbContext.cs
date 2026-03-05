using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace TodoApi;

public partial class ToDoDbContext : DbContext
{
    public ToDoDbContext()
    {
    }

    public ToDoDbContext(DbContextOptions<ToDoDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Item> Items { get; set; }
    public virtual DbSet<User> Users { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql("name=ToDoDB", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.44-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // הגדרות כלליות של מסד הנתונים (Charset ו-Collation)
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        // הגדרת ישות המשימות (Item)
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("item"); // מוודא ששם הטבלה ב-SQL הוא item

            entity.Property(e => e.Name)
                  .HasMaxLength(100)
                  .IsRequired(); // שם המשימה הוא שדה חובה

            // --- הוספת הקשר למשתמש (Foreign Key) ---
            entity.HasOne(i => i.User)           // למשימה יש משתמש אחד
                  .WithMany(u => u.Items)        // למשתמש יש הרבה משימות
                  .HasForeignKey(i => i.UserId)  // השדה המקשר
                  .OnDelete(DeleteBehavior.Cascade) // מחיקת משתמש תמחוק את משימותיו
                  .HasConstraintName("FK_Item_User"); // שם למפתח הזר ב-SQL
        });

        // הגדרת ישות המשתמשים (אופציונלי - כדי לוודא ששם הטבלה תואם)
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users"); 
            entity.HasKey(e => e.Id);
        });

        OnModelCreatingPartial(modelBuilder);
    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

}
