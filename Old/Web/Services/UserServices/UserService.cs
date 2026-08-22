using CoreLib.Common;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Client.Feature.UserManagements;
using Web.Client.Shared.Models;
using Web.Common.Database;
using Web.Components.ForgetPassword;

namespace Web.Services.UserServices;

public class UserService : IUser
{
    private readonly AppDbContext _appDbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager, AppDbContext appDbContext)
    {
        _userManager = userManager;
        _appDbContext = appDbContext;
    }

    public async Task<PaginatedResponse<ApplicationUser>> Find(FindRequest r, CancellationToken ct)
    {
        var data = await _appDbContext
            .Users
            .Include(x => x.Kota)
            .ThenInclude(y => y.Provinsi)
            .WhereIf(!string.IsNullOrEmpty(r.Search),
                x => x.NamaLengkap.ToLower()
                    .Contains(r.Search.ToLower()))
            .OrderBy(x => x.UserName)
            .ToPaginatedList(r.Page, r.PageSize, r.OrderBy, r.Direction, ct);
        return data;
    }

    public async Task<CreatedResponse<UserDTO>> Register(UserAddDTO r, CancellationToken? ct)
    {
        var finduser = await _userManager.FindByNameAsync(r.UserName);
        if (finduser != null) return new CreatedResponse<UserDTO>(false, "Username sudah ada");
        var nohp = _userManager.Users.FirstOrDefault(x => x.PhoneNumber == r.PhoneNumber);
        if (nohp != null) return new CreatedResponse<UserDTO>(false, "Nomor Telepon Sudah Digunakan");
        var user = new ApplicationUser
        {
            NamaLengkap = r.NamaLengkap,
            UserName = r.UserName,
            Email = r.Email,
            Gender = r.Gender,
            PhoneNumber = r.PhoneNumber,
            Photo = r.Photo,
            Pekerjaan = r.Pekerjaan,
            KotaId = r.KotaId,
            CreatedDate = DateTime.UtcNow
        };
        var createUser = await _userManager.CreateAsync(user, r.Password);
        if (createUser.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, r.Role);
            return new CreatedResponse<UserDTO>(true, "Berhasil membuat user", user.Adapt<UserDTO>());
        }

        return new CreatedResponse<UserDTO>(false, createUser.Errors.First().Description);
    }

    public async Task<ServiceResponse> Update(UserEditDTO r, CancellationToken ct)
    {
        var finduser = await _userManager.FindByNameAsync(r.UserName);
        var oldData = finduser.Adapt<UserDTO>();
        var rol = await _userManager.GetRolesAsync(finduser);
        oldData.Role = rol.First();

        finduser.NamaLengkap = r.NamaLengkap;
        finduser.Email = r.Email;
        finduser.Gender = r.Gender;
        finduser.PhoneNumber = r.PhoneNumber;
        finduser.Photo = r.Photo;
        finduser.Pekerjaan = r.Pekerjaan;
        finduser.KotaId = r.KotaId;
        finduser.LastLogin = r.LastLogin;
        finduser.EmailConfirmed = r.EmailConfirmed;
        var updated = await _userManager.UpdateAsync(finduser);
        if (oldData.Role != r.Role)
        {
            await _userManager.RemoveFromRoleAsync(finduser, oldData.Role);
            await _userManager.AddToRoleAsync(finduser, r.Role);
        }

        if (updated.Succeeded)
            return new ServiceResponse(true, "Berhasil mengupdate user");
        return new ServiceResponse(false, updated.Errors.First().Description);
    }

    public async Task<ServiceResponse> Delete(string id, CancellationToken? ct)
    {
        var finduser = await _userManager.FindByIdAsync(id);
        await _userManager.DeleteAsync(finduser);
        return new ServiceResponse(true, "Berhasil menghapus user");
    }

    public async Task<UserDTO> Get(string id)
    {
        var finduser = await _userManager.FindByIdAsync(id);
        var data = finduser.Adapt<UserDTO>();
        var rol = await _userManager.GetRolesAsync(finduser);
        data.Role = rol.First();
        return data;
    }

    public async Task<ServiceResponse> ForceActivate(string userID)
    {
        var finduser = await _userManager.FindByIdAsync(userID);
        finduser.EmailConfirmed = true;
        var updated = await _userManager.UpdateAsync(finduser);
        if (updated.Succeeded)
            return new ServiceResponse(true, "Berhasil mengaktifkan user");
        return new ServiceResponse(false, updated.Errors.First().Description);
    }

    public async Task<ServiceResponse> ResetPassword(PasswordReset data)
    {
        var finduser = await _userManager.FindByIdAsync(data.UserID);
        var res = await _userManager.ResetPasswordAsync(finduser, data.Token, data.ConfirmPassword);
        if (res.Succeeded)
            return new ServiceResponse(true, "Berhasil Mengubah Password");
        return new ServiceResponse(false, res.Errors.First().Description);
    }

    public async Task<string> GenerateResetPassword(string userID)
    {
        var finduser = await _userManager.FindByIdAsync(userID);
        var token = await _userManager.GeneratePasswordResetTokenAsync(finduser);
        return token;
    }

    public async Task<ServiceResponse> UpdateLastLogin(string userID, CancellationToken ct)
    {
        var finduser = await _userManager.FindByIdAsync(userID);
        finduser.LastLogin = DateTime.UtcNow;
        await _appDbContext.SaveChangesAsync(ct);
        return new ServiceResponse(true, "Berhasil Login");
    }

    public async Task<ValidUserResponse> ValidateUserReset(validuserDTO r)
    {
        var finduser = await _userManager.FindByNameAsync(r.Username);
        if (finduser == null)
            return new ValidUserResponse
            {
                Valid = false,
                Message = "Username tidak ditemukan"
            };

        if (finduser.PhoneNumber != r.NomorHP)
            return new ValidUserResponse
            {
                Valid = false,
                Message = "Nomor HP tidak sesuai"
            };

        return new ValidUserResponse
        {
            Valid = true,
            Username = finduser.UserName,
            Email = finduser.Email,
            Id = finduser.Id
        };
    }
}