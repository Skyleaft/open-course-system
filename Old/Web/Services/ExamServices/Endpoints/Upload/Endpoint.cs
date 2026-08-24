using CoreLib.Common;

namespace Web.Services.ExamServices.Endpoints.Upload;

public class Endpoint(IDocx docx):Endpoint<Request,DocxResult>
{
    public override void Configure()
    {
        Post("/exam/upload");
        AllowFileUploads();
    }


    public override async Task HandleAsync(Request r, CancellationToken ct)
    {
        if (r.File == null || r.File.Length == 0)
            await SendErrorsAsync(cancellation: ct);
        try
        {
            using var stream = new MemoryStream();
            await r.File.CopyToAsync(stream, ct);
            stream.Position = 0;

            var result = await docx.ProcessDocxAsync(stream, r.File.FileName);
            await SendAsync(result, cancellation: ct);
        }
        catch (Exception ex)
        {
            ThrowError($"{ex.Message}");
            return;
        }
    }
}