using System.ComponentModel.DataAnnotations;

namespace Web.Components.Auth;

public class LoginDTO
{
    [Required]
    [MinLength(4)]
    [MaxLength(40)]
    public string UserName { get; set; } = null!;

    [Required]
    [MinLength(6)]
    [MaxLength(30)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}