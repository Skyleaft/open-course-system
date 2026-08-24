using Web.Client.Shared.Models;

namespace Web.Services.UserExamServices.Endpoints.RetryExam;

public class Endpoint : EndpointWithoutRequest<ServiceResponse>
{
    private readonly IUserExam _repo;

    public Endpoint(IUserExam examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Get("/UserExam/retry/{Id}");
    }


    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _repo.RetryExam(Route<Guid>("Id"), ct);
        await SendAsync(res, cancellation: ct);
    }
}