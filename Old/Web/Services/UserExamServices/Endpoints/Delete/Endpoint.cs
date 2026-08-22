using Web.Client.Shared.Models;

namespace Web.Services.UserExamServices.Endpoints.Delete;

public class Endpoint : EndpointWithoutRequest<ServiceResponse>
{
    private readonly IUserExam _repo;

    public Endpoint(IUserExam examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Delete("/UserExam/{Id}");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _repo.Delete(Route<Guid>("Id"));
        await SendAsync(res, cancellation: ct);
    }
}