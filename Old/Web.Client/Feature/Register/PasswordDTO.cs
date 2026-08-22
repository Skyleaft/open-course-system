using System.ComponentModel.DataAnnotations;

namespace Web.Client.Feature.Register;

public class PasswordDTO
{
    [Required(ErrorMessage = "Password Tidak Boleh Kosong")]
    [DataType(DataType.Password)]
    [StringLength(30, ErrorMessage = "Password Minimal {2} karakter dan Maksimal {1}", MinimumLength = 6)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Password Tidak Boleh Kosong")]
    [StringLength(30, ErrorMessage = "Password Minimal {2} karakter dan Maksimal {1}", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Compare("Password")]
    public string ConfirmPassword { get; set; }
}