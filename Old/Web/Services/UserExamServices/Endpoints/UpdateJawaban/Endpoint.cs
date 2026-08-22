using Web.Client.Shared.Models;

namespace Web.Services.UserExamServices.Endpoints.UpdateJawaban;

public class Endpoint : Endpoint<UpdateJawabanDTO, ServiceResponse>
{
    private readonly IUserExam _repo;

    public Endpoint(IUserExam examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Put("/UserExam/UserAnswer/{Id}");
    }


    public override async Task HandleAsync(UpdateJawabanDTO r, CancellationToken ct)
    {
        var res = await _repo.UpdateJawaban(r, ct);
        await SendAsync(res, cancellation: ct);
    }
}