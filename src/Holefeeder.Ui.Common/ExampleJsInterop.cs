using Microsoft.JSInterop;

namespace Holefeeder.Ui.Common;

// This class provides an example of how JavaScript functionality can be wrapped
// in a .NET class for easy consumption. The associated JavaScript module is
// loaded on demand when first needed.
//
// This class can be registered as scoped DI service and then injected into Blazor
// components for use.

public class ExampleJsInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
        "import", "./_content/Holefeeder.Ui.Components/exampleJsInterop.js").AsTask());

    public async ValueTask<string> Prompt(string message)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("showPrompt", message);
    }

#pragma warning disable CA1816
    public async ValueTask DisposeAsync()
#pragma warning restore CA1816
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
