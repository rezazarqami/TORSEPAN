using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TORSEPAN.Panel.Components.Shared;

var output = Environment.GetEnvironmentVariable("WORKSPACE_HEADER_PREVIEW_DIR") ?? throw new InvalidOperationException("Set WORKSPACE_HEADER_PREVIEW_DIR.");
Directory.CreateDirectory(output);
var services = new ServiceCollection().AddLogging();
await using var provider = services.BuildServiceProvider();
await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
foreach (var kind in new[] { "account", "messages" })
{
    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        var rendered = await renderer.RenderComponentAsync<WorkspacePhotoHero>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["CssClass"] = kind == "account" ? "account-hero" : "messages-hero",
            ["Kind"] = kind,
            ["Title"] = kind == "account" ? "ناحیهٔ کاربری" : "مدیریت پیام‌ها",
            ["Eyebrow"] = kind == "account" ? "TORSEPAN · MY ACCOUNT" : "WORKSHOP · MESSAGES",
            ["Description"] = kind == "account" ? "همکار تورسپن · عملکرد و تنظیمات حساب شما" : "اطلاعیه‌های کارگاه و گفتگوهای اعضا، در یک فضای مشترک"
        }));
        await File.WriteAllTextAsync(Path.Combine(output, kind + ".html"), rendered.ToHtmlString());
        Console.WriteLine("Rendered actual WorkspacePhotoHero: " + kind);
    });
}
