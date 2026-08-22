using System.ComponentModel.DataAnnotations;

namespace Web.Components.ForgetPassword;

public class validuserDTO
{
    [StringLength(30)]
    [Required(ErrorMessage = "Username Tidak Boleh Kosong")]
    public string Username { get; set; }

    [StringLength(15)]
    [Required(ErrorMessage = "Nomor HP Tidak Boleh Kosong")]
    public string NomorHP { get; set; }
}