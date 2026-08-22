using System.ComponentModel.DataAnnotations;
using CoreLib.Common;
using Microsoft.AspNetCore.Identity;

namespace CoreLib.Users;

public class ApplicationUser : IdentityUser
{
    public string NamaLengkap { get; set; }

    [Required] public Gender Gender { get; set; }

    public byte[]? Photo { get; set; }
    public string? Pekerjaan { get; set; }
    public Kota? Kota { get; set; }
    public string? KotaId { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastLogin { get; set; }
}