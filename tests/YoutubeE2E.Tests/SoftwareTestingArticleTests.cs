using Microsoft.Playwright;
using NUnit.Framework;

namespace YoutubeE2E.Tests;

public sealed partial class YoutubeSearchTests
{
    private const string SoftwareTestingArticleUrl = "https://www.ibm.com/mx-es/think/topics/software-testing";

    [Test]
    public async Task SoftwareTestingArticleDisplaysExpectedTitle()
    {
        const string expectedTitle = "¿Qué son las pruebas de software?";

        await Page.GotoAsync(SoftwareTestingArticleUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });

        var heading = Page.Locator("h1").First;
        await heading.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });

        var actualTitle = string.Join(' ', (await heading.InnerTextAsync()).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        _videoTitle = actualTitle;
        _videoUrl = Page.Url;

        Assert.That(actualTitle, Is.EqualTo(expectedTitle), "El encabezado principal debe coincidir con el título de la captura.");
    }
}