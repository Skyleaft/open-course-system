using System.ComponentModel.DataAnnotations;

namespace Web.Client.Feature.UserManagements;

public class UserAddDTO : UserDTO
{
    [Required(ErrorMessage = "Username Tidak Boleh Kosong")]
    [StringLength(40, ErrorMessage = "Username Minimal {2} karakter dan Maksimal {1}", MinimumLength = 4)]
    public override string UserName { get; set; } = null!;

    [Required(ErrorMessage = "Password Tidak Boleh Kosong")]
    [StringLength(30, ErrorMessage = "Password Minimal {2} karakter dan Maksimal {1}", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}