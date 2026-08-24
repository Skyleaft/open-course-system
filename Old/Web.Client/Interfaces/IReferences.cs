using CoreLib.Users;

namespace Web.Client.Interfaces;

public interface IReferences
{
    public Task<List<Provinsi>> GetAllprovinsi();
    public Task<List<Kota>> GetAllKota();
}