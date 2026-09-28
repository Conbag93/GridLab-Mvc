using System.ComponentModel.DataAnnotations;

namespace GridLab.Shared.Models;

public sealed class Category : IEntity
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = "";
}
