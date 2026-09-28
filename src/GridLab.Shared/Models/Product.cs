using System.ComponentModel.DataAnnotations;

namespace GridLab.Shared.Models;

public sealed class Product : IEntity
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = "";

    [Display(Name = "Category")]
    [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
    public int CategoryId { get; set; }

    [Display(Name = "Unit price")]
    [Range(0, 100_000)]
    public decimal UnitPrice { get; set; }

    [Display(Name = "In stock")]
    [Range(0, int.MaxValue)]
    public int UnitsInStock { get; set; }

    public bool Discontinued { get; set; }

    [Display(Name = "Last updated")]
    public DateTime LastUpdated { get; set; }
}
