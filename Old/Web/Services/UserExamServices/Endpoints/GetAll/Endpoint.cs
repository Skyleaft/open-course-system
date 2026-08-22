using CoreLib.TakeExam;

namespace Web.Services.UserExamServices.Endpoints.GetAll;

public class Endpoint : EndpointWithoutRequest<IEnumerable<UserExam>>
{
    private readonly IUserExam _repo;

    public Endpoint(IUserExam examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Get("/UserExam/get-all/{Id}");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _repo.GetAll(Route<Guid>("Id"), ct);
        await SendAsync(res, cancellation: ct);
    }
}