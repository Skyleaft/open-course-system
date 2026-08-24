using CoreLib.BankSoal;
using Web.Client.Shared.Models;

namespace Web.Services.ExamServices.Endpoints.Update;

public class Endpoint : Endpoint<Exam, ServiceResponse>
{
    private readonly IExam _examService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Endpoint(IExam examService, IHttpContextAccessor httpContextAccessor)
    {
        _examService = examService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override void Configure()
    {
        Put("/exam/{Id}");
    }


    public override async Task HandleAsync(Exam r, CancellationToken ct)
    {
        var username = _httpContextAccessor.HttpContext.User.Identity.Name;
        r.LastModifiedBy = username;
        var res = await _examService.Update(r, ct);
        await SendAsync(res, cancellation: ct);
    }
}