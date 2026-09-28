using System.ComponentModel.DataAnnotations;

namespace GridLab.Shared.Models;

public enum UserRole { Viewer, Editor, Admin }

public sealed class User : IEntity
{
    public int Id { get; set; }

    [Required, StringLength(40), Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required, StringLength(40), Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, StringLength(120)]
    public string Email { get; set; } = "";

    public UserRole Role { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created"), Editable(false)]
    public DateTime CreatedOn { get; set; }

    [Display(Name = "Last login")]
    public DateTime? LastLogin { get; set; }
}
