using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreLib.Users;

[Table("Kota", Schema = "reference")]
public class Kota
{
    [Key] [StringLength(100)] public string Id { get; set; }

    public Provinsi Provinsi { get; set; }
    public string ProvinsiId { get; set; }
    public string NamaKota { get; set; }
}