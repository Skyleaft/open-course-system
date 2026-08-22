using CoreLib.Common;
using Web.Services.ExamServices.Endpoints.Upload;

namespace Web.Services.ExamServices.Endpoints.Download;

public class Endpoint(IDocx docx):EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/exam/download");
    }


    public override async Task HandleAsync(CancellationToken ct)
    {
        
    }
}