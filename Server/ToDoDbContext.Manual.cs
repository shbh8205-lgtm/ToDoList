using Microsoft.EntityFrameworkCore;

namespace TodoApi;

public partial class ToDoDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasOne(i => i.User)
                  .WithMany(u => u.Items)
                  .HasForeignKey(i => i.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_Item_User");
        });
    }
}