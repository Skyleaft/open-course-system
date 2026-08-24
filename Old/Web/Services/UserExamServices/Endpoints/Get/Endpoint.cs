using CoreLib.TakeExam;

namespace Web.Services.UserExamServices.Endpoints.Get;

public class Endpoint : EndpointWithoutRequest<UserExam>
{
    private readonly IUserExam _repo;

    public Endpoint(IUserExam examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Get("/UserExam/{Id}");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _repo.Get(Route<Guid>("Id"), ct);
        await SendAsync(res, cancellation: ct);
    }
}