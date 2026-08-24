using Microsoft.EntityFrameworkCore;
using Web.Common.Database;

namespace Web.Services.ReferenceServices.Endpoints.GetAllProvinsi;

public class Endpoint(AppDbContext repo) : EndpointWithoutRequest<List<Provinsi>>
{
    public override void Configure()
    {
        Get("/ref/provinsi/all");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await repo.Provinsi.ToListAsync(ct);
        await SendAsync(res, cancellation: ct);
    }
}