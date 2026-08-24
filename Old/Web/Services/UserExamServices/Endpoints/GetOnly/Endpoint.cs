using CoreLib.TakeExam;

namespace Web.Services.UserExamServices.Endpoints.GetOnly;

public class Endpoint : EndpointWithoutRequest<UserExam>
{
    private readonly IUserExam _repo;

    public Endpoint(IUserExam examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Get("/UserExam/getonly/{Id}");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _repo.GetOnly(Route<Guid>("Id"));
        await SendAsync(res, cancellation: ct);
    }
}