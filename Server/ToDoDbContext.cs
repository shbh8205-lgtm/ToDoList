using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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
    {
        if (!optionsBuilder.IsConfigured)
        {
            // במקום להסתמך על "name=", נשתמש במחרוזת ישירה או נמשוך אותה בצורה מפורשת
            // לצורך יצירת המיגרציה, EF צריך לדעת באיזה Database Driver להשתמש.
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 31));

            // כאן תוכל לשים את מחרוזת החיבור המקומית שלך זמנית, 
            // או להשתמש בפורמט בטוח יותר:
            optionsBuilder.UseMySql("Server=localhost;Database=ToDoDB;Uid=root;Pwd=root;", serverVersion);
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // הגדרות Charset ו-Collation בצורה שתואמת את כל הגרסאות
        modelBuilder.HasCharSet("utf8mb4");

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("item");

            entity.Property(e => e.Id).ValueGeneratedOnAdd(); // זה ה-Auto Increment התקני

            entity.Property(e => e.TaskName)
                  .HasMaxLength(100)
                  .IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        // קריאה למתודה החלקית מהקובץ Manual שפתחנו
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}