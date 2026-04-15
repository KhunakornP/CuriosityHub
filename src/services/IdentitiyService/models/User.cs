using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IdentitiyService.Models.Enums;

namespace IdentitiyService.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Viewer;

    // Navigation properties
    public PasswordAuth? PasswordAuth { get; set; }
    public GoogleAuth? GoogleAuth { get; set; }
    public Profile? Profile { get; set; }
}