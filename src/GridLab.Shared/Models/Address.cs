using System.ComponentModel.DataAnnotations;

namespace GridLab.Shared.Models;

public enum AddressKind { Home, Work, Billing, Delivery }

public sealed class Address : IEntity
{
    public int Id { get; set; }

    [Display(Name = "User"), Range(1, int.MaxValue, ErrorMessage = "A user id is required.")]
    public int UserId { get; set; }

    public AddressKind Kind { get; set; }

    [Required, StringLength(120), Display(Name = "Address line")]
    public string Line1 { get; set; } = "";

    [Required, StringLength(60)]
    public string City { get; set; } = "";

    [Required, StringLength(12), Display(Name = "Post code")]
    public string PostCode { get; set; } = "";

    [Required, StringLength(2)]
    public string Country { get; set; } = "GB";

    [Display(Name = "Primary")]
    public bool IsPrimary { get; set; }
}
