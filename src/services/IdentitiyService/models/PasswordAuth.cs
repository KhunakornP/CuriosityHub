using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentitiyService.Models;

public class PasswordAuth
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}