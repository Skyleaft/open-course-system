using Microsoft.EntityFrameworkCore;
using Web.Common.Database;

namespace Web.Services.ReferenceServices.Endpoints.GetAllKota;

public class Endpoint(AppDbContext repo) : EndpointWithoutRequest<List<Kota>>
{
    public override void Configure()
    {
        Get("/ref/kota/all");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await repo.Kota.ToListAsync(ct);
        await SendAsync(res, cancellation: ct);
    }
}