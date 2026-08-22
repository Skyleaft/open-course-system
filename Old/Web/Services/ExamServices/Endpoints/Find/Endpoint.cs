using CoreLib.BankSoal;
using CoreLib.Common;

namespace Web.Services.ExamServices.Endpoints.Find;

public class Endpoint : Endpoint<FindRequest, PaginatedResponse<Exam>>
{
    private readonly IExam _examService;

    public Endpoint(IExam examService)
    {
        _examService = examService;
    }

    public override void Configure()
    {
        Post("/exam/Find");
    }

    public override async Task HandleAsync(FindRequest r, CancellationToken ct)
    {
        var res = await _examService.Find(r, ct);
        await SendAsync(res, cancellation: ct);
    }
}