using Microsoft.AspNetCore.Components.Forms;

namespace Web.Client.Feature.UserManagements;

public class UserEditDTO : UserDTO
{
    public IBrowserFile ImageFile { get; set; }
    public DateTime? LastLogin { get; set; }
}