using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentitiyService.Models;

public class GoogleAuth
{
    [Key]
    public string GoogleId { get; set; } = string.Empty; // gid

    [Required]
    public Guid UserId { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}