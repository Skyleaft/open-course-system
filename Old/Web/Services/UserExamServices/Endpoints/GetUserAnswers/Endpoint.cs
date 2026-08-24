using CoreLib.TakeExam;

namespace Web.Services.UserExamServices.Endpoints.GetUserAnswers;

public class Endpoint : EndpointWithoutRequest<List<UserAnswer>>
{
    private readonly IUserExam _repo;

    public Endpoint(IUserExam examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Get("/UserExam/{Id}/UserAnswer/");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _repo.GetUserAnswers(Route<Guid>("Id"), ct);
        await SendAsync(res, cancellation: ct);
    }
}