using CoreLib.BankSoal;
using CoreLib.Common;

namespace Web.Services.ExamServices.Endpoints.Create;

public class Endpoint : Endpoint<Exam, CreatedResponse<Exam>>
{
    private readonly IExam _examService;

    public Endpoint(IExam examService)
    {
        _examService = examService;
    }

    public override void Configure()
    {
        Post("/exam");
    }

    public override async Task HandleAsync(Exam r, CancellationToken ct)
    {
        var res = await _examService.Create(r, ct);
        await SendCreatedAtAsync($"/exam/{res.Data.Id}", res.Data, res, cancellation: ct);
    }
}