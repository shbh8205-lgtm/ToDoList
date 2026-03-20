using System;
using System.Collections.Generic;

namespace TodoApi;

public class Item
{
    public int Id { get; set; }
    public string TaskName { get; set; }
    public bool IsComplete { get; set; }

    // שדה המפתח הזר
    public int UserId { get; set; }

    // מאפיין ניווט - מאפשר ל-EF להבין את הקשר
    public User? User { get; set; } 
}