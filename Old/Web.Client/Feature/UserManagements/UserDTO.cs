using System.ComponentModel.DataAnnotations;
using CoreLib.Common;

namespace Web.Client.Feature.UserManagements;

public class UserDTO
{
    public Guid Id { get; set; }
    public virtual string UserName { get; set; }

    [Required(ErrorMessage = "Nama Lengkap Harus Diiisi")]
    [StringLength(50, ErrorMessage = "Username Minimal {2} karakter dan Maksimal {1}", MinimumLength = 3)]
    public string NamaLengkap { get; set; }

    [Required] public Gender Gender { get; set; }

    [Required(ErrorMessage = "Pekerjaan Wajib Diisi")]
    public string Pekerjaan { get; set; }

    public byte[]? Photo { get; set; }

    [Required(ErrorMessage = "Email Wajib Diisi")]
    [EmailAddress(ErrorMessage = "Format Email Salah")]
    public string Email { get; set; }

    [MaxLength(14)] public string? PhoneNumber { get; set; }

    public string Role { get; set; }

    [Required(ErrorMessage = "Asal Kota harus Diisi")]
    public string KotaId { get; set; }

    public bool EmailConfirmed { get; set; }
}