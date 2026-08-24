using CoreLib.TakeExam;
using Web.Client.Shared.Models;

namespace Web.Services.UserExamServices.Endpoints.Update;

public class Endpoint : Endpoint<UserExam, ServiceResponse>
{
    private readonly IUserExam _repo;

    public Endpoint(IUserExam examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Put("/UserExam/{Id}");
    }


    public override async Task HandleAsync(UserExam r, CancellationToken ct)
    {
        var res = await _repo.Update(r, ct);
        await SendAsync(res, cancellation: ct);
    }
}