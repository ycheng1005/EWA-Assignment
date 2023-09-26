using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Models;

public class DB : DbContext
{
    public DB(DbContextOptions<DB> options) : base(options) { }

    // DB Sets
    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
}

// Entity Classes

#nullable disable warnings

public class User
{
    [Key, MaxLength(100)]
    public string Email { get; set; }
    [MaxLength(100)]
    public string Hash { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }

    [NotMapped]
    public string Role => GetType().Name;
}

public class Admin : User
{

}

public class Member : User
{
    [MaxLength(100)]
    public string PhotoURL { get; set; }

    // Navigation
    public List<Order> Orders { get; set; } = new();
}

// ----------------------------------------------------------------------------

public class Product
{
    [Key, MaxLength(4)]
    public string Id { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    [Precision(6, 2)]
    public decimal Price { get; set; }
    [MaxLength(100)]
    public string PhotoURL { get; set; }

    // Navigation
    public List<OrderLine> OrderLines { get; set; } = new();
}

public class Order
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Column(TypeName = "DATE")]
    public DateTime Date { get; set; }

    // FK
    public string MemberEmail { get; set; }

    // Navigation
    public Member Member { get; set; }
    public List<OrderLine> OrderLines { get; set; } = new();
}

public class OrderLine
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Precision(6, 2)]
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    // FK
    public int OrderId { get; set; }
    public string ProductId { get; set; }

    // Navigation
    public Order Order { get; set; }
    public Product Product { get; set; }
}
