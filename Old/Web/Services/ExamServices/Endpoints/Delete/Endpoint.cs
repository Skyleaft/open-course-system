using Web.Client.Shared.Models;

namespace Web.Services.ExamServices.Endpoints.Delete;

public class Endpoint : EndpointWithoutRequest<ServiceResponse>
{
    private readonly IExam _examService;

    public Endpoint(IExam examService)
    {
        _examService = examService;
    }

    public override void Configure()
    {
        Delete("/exam/{Id}");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _examService.Delete(Route<int>("Id"));
        await SendAsync(res, cancellation: ct);
    }
}