using CoreLib.Report;

namespace Web.Services.ReportServices.Endpoints.Get;

public class Endpoint : EndpointWithoutRequest<ExamReport>
{
    private readonly IReport _repo;

    public Endpoint(IReport repo)
    {
        _repo = repo;
    }

    public override void Configure()
    {
        Get("/report/{Id}");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _repo.Get(Route<Guid>("Id"), ct);
        await SendAsync(res, cancellation: ct);
    }
}