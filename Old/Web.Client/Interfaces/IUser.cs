using CoreLib.Common;
using CoreLib.Users;
using Web.Client.Feature.UserManagements;
using Web.Client.Shared.Models;
using Web.Components.ForgetPassword;

namespace Web.Client.Interfaces;

public interface IUser
{
    public Task<PaginatedResponse<ApplicationUser>> Find(FindRequest r, CancellationToken ct);
    public Task<CreatedResponse<UserDTO>> Register(UserAddDTO r, CancellationToken? ct);
    public Task<ServiceResponse> Update(UserEditDTO r, CancellationToken ct);
    public Task<ServiceResponse> Delete(string id, CancellationToken? ct);
    public Task<UserDTO> Get(string id);
    public Task<ServiceResponse> ForceActivate(string userID);
    public Task<ServiceResponse> ResetPassword(PasswordReset data);
    public Task<string> GenerateResetPassword(string userID);
    public Task<ServiceResponse> UpdateLastLogin(string userID, CancellationToken ct);
    public Task<ValidUserResponse> ValidateUserReset(validuserDTO r);
}