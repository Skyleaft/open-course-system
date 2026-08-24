using BlazorJS;
using Microsoft.JSInterop;
using MudExRichTextEditor;
using MudExRichTextEditor.Extensibility;
using MudExRichTextEditor.Types;

namespace Web.Client.Shared.Extensions;

public class QuilImageCompress : IQuillModule
{
    public IJSObjectReference JsReference { get; }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public Task<object> GetQuillJsCreationConfigAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        return Task.FromResult<object>(new
        {
            imageCompress = new
            {
                quality = 0.1,
                maxWidth = 100,
                maxHeight = 100
            }
        });
    }

    public async Task<IJSObjectReference> OnLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        var res = await jsRuntime.ImportModuleAsync("/js/quill.imageCompressor.min.js");
        return res;
    }

    public Task OnCreatedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        return Task.CompletedTask;
    }

    public string[] JsFiles => new[] { "/js/quill.imageCompressor.min.js" };
    public string[] CssFiles => Array.Empty<string>();
    public string JsConfigFunction => null;
    public IEnumerable<QuillTool> Tools { get; }
}