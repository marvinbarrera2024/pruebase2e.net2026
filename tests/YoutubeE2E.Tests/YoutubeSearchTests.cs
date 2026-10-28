using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace YoutubeE2E.Tests;

[TestFixture]
public sealed partial class YoutubeSearchTests : PageTest
{
    private const string DefaultQuery = "Jira";
    private const string YoutubeVideoUrl = "https://www.youtube.com/watch?v=LeRxpTy-eNU";
    private const double PlaybackTargetSeconds = 5 * 60;
    private DateTimeOffset _startedAt;
    private string? _videoTitle;
    private string? _videoUrl;
    private string? _failureScreenshot;

    [OneTimeSetUp]
    public void InitializeReport()
    {
        ReportWriter.Reset();
    }

    [SetUp]
    public void InitializeTestResult()
    {
        _startedAt = DateTimeOffset.Now;
        _videoTitle = null;
        _videoUrl = null;
        _failureScreenshot = null;
    }

    [Test]
    public async Task SearchReturnsAtLeastOneVideo()
    {
        var query = Environment.GetEnvironmentVariable("YOUTUBE_SEARCH_QUERY") ?? DefaultQuery;
        var searchUrl = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}";

        await Page.GotoAsync(searchUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });

        var firstVideo = Page.Locator("ytd-video-renderer a#video-title").First;
        await firstVideo.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });

        _videoTitle = (await firstVideo.InnerTextAsync()).Trim();
        var resultUrl = await firstVideo.GetAttributeAsync("href");
        _videoUrl = resultUrl is null ? null : new Uri(new Uri(Page.Url), resultUrl).ToString();

        Assert.That(_videoTitle, Is.Not.Empty, "El primer resultado debe tener título.");
        Assert.That(_videoUrl, Does.Contain("/watch?v="), "El primer resultado debe enlazar a un video.");
    }

    [Test]
    public async Task VideoContinuesPlayingAfterSeekingFiveMinutes()
    {
        await Page.GotoAsync(YoutubeVideoUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });

        var player = Page.Locator("video.html5-main-video");
        await player.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });

        var pageTitle = await Page.TitleAsync();
        _videoTitle = string.IsNullOrWhiteSpace(pageTitle) || pageTitle == "- YouTube"
            ? "Video de YouTube LeRxpTy-eNU"
            : pageTitle;
        _videoUrl = Page.Url;

        try
        {
            await Page.WaitForFunctionAsync(
                "() => { const video = document.querySelector('video.html5-main-video'); return video && video.readyState >= 1 && Number.isFinite(video.duration); }",
                options: new PageWaitForFunctionOptions { Timeout = 10_000 });
        }
        catch (TimeoutException)
        {
            var pageText = await Page.Locator("body").InnerTextAsync();
            var reason = pageText.Contains("confirm you're not a bot", StringComparison.OrdinalIgnoreCase)
                ? "YouTube solicitó confirmar que no eres un bot; el reproductor no está disponible para esta ejecución."
                : "El reproductor de YouTube no cargó los metadatos del video.";
            Assert.Fail(reason);
        }

        var duration = await player.EvaluateAsync<double>("video => video.duration");
        Assert.That(duration, Is.GreaterThan(PlaybackTargetSeconds), "El video debe durar más de cinco minutos para realizar esta prueba.");

        await player.EvaluateAsync<bool>($"async video => {{ video.muted = true; video.currentTime = {PlaybackTargetSeconds}; await video.play(); return true; }}");
        await Page.WaitForFunctionAsync(
            $"() => {{ const video = document.querySelector('video.html5-main-video'); return video && video.currentTime >= {PlaybackTargetSeconds}; }}",
            options: new PageWaitForFunctionOptions { Timeout = 15_000 });

        var seekPosition = await player.EvaluateAsync<double>("video => video.currentTime");
        await Page.WaitForFunctionAsync(
            $"() => {{ const video = document.querySelector('video.html5-main-video'); return video && !video.paused && video.currentTime > {seekPosition + 1}; }}",
            options: new PageWaitForFunctionOptions { Timeout = 15_000 });

        var isPlaying = await player.EvaluateAsync<bool>("video => !video.paused && video.currentTime > 301");
        Assert.That(isPlaying, Is.True, "El video debe seguir reproduciéndose después de avanzar cinco minutos.");
    }

    [TearDown]
    public async Task CaptureFailureScreenshot()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
        {
            var artifactsDirectory = ReportWriter.GetArtifactsDirectory();
            Directory.CreateDirectory(artifactsDirectory);
            var safeTestName = string.Join('-', TestContext.CurrentContext.Test.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            _failureScreenshot = Path.Combine(artifactsDirectory, $"{safeTestName}-failure.png");

            try
            {
                await Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = _failureScreenshot,
                    FullPage = true
                });
            }
            catch (PlaywrightException)
            {
                _failureScreenshot = null;
            }
        }

        WriteHtmlReport();
    }

    [OneTimeTearDown]
    public void WriteFallbackReport()
    {
        if (!ReportWriter.HasResults)
        {
            WriteHtmlReport();
        }
    }

    private void WriteHtmlReport()
    {
        var result = TestContext.CurrentContext.Result;
        ReportWriter.Write(
            result.Outcome.Status,
            TestContext.CurrentContext.Test.Name,
            (DateTimeOffset.Now - _startedAt).TotalSeconds,
            result.Message,
            _startedAt,
            _videoTitle,
            _videoUrl,
            _failureScreenshot);
    }
}
