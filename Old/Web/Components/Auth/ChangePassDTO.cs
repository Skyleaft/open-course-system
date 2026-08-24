using System.ComponentModel.DataAnnotations;

namespace Web.Components.Auth;

public class ChangePassDTO
{
    [Required]
    [MinLength(6)]
    [MaxLength(30)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Required]
    [MinLength(6)]
    [MaxLength(30)]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = null!;
}