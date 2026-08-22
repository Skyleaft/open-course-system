using CoreLib.BankSoal;

namespace Web.Services.ExamServices.Endpoints.Get;

public class Endpoint : EndpointWithoutRequest<Exam>
{
    private readonly IExam _examService;

    public Endpoint(IExam examService)
    {
        _examService = examService;
    }

    public override void Configure()
    {
        Get("/exam/{Id}");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _examService.Get(Route<int>("Id"));
        await SendAsync(res, cancellation: ct);
    }
}