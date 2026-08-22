using CoreLib.Common;
using CoreLib.RoomSet;
using Web.Client.Shared.Models;

namespace Web.Client.Interfaces;

public interface IRoom
{
    public Task<CreatedResponse<Room>> Create(Room r);
    public Task<ServiceResponse> Update(Room r);
    public Task<ServiceResponse> Delete(Guid Id);
    public Task<Room> Get(Guid Id, CancellationToken ct);
    public Task<Room> Get(string kode);
    public Task<RoomView> GetRoomOnly(Guid Id, CancellationToken ct);
    public Task<PaginatedResponse<Room>> Find(FindRequest r, CancellationToken ct, string? Username = "");
    public Task<PaginatedResponse<RoomExam>> FindRoomView(FindRequest r, CancellationToken ct, string? Username = "");
    public Task<IEnumerable<Room>> AllRefference(CancellationToken ct);
}