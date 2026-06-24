using System.ComponentModel.DataAnnotations;

namespace PharmacyApp.Models;

public class Medicine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MinLength(2)]
    public string FullName { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    [Required]
    public DateTime? ExpiryDate { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    public string Brand { get; set; } = string.Empty;
}
