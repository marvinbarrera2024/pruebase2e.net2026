using System.Net;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace YoutubeE2E.Tests;

internal static class ReportWriter
{
  private sealed record CaseResult(
    TestStatus Status,
    string Name,
    double DurationSeconds,
    string? Message,
    DateTimeOffset StartedAt,
    string? Title,
    string? Url,
    string? ScreenshotPath);

  private static readonly object SyncRoot = new();
  private static readonly List<CaseResult> Results = [];

    public static string GetArtifactsDirectory()
    {
        var configuredDirectory = Environment.GetEnvironmentVariable("E2E_ARTIFACTS_DIR");
        return Path.GetFullPath(configuredDirectory ?? Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts"));
    }

  public static bool HasResults
  {
    get
    {
      lock (SyncRoot)
      {
        return Results.Count > 0;
      }
    }
  }

  public static void Reset()
  {
    lock (SyncRoot)
    {
      Results.Clear();
    }
  }

    public static void Write(
        TestStatus status,
        string name,
        double resultDuration,
        string? message,
        DateTimeOffset startedAt,
        string? videoTitle,
        string? videoUrl,
        string? screenshotPath)
    {
        CaseResult[] results;
        lock (SyncRoot)
        {
          Results.RemoveAll(result => result.Name == name);
          Results.Add(new CaseResult(status, name, resultDuration, message, startedAt, videoTitle, videoUrl, screenshotPath));
          results = Results.OrderBy(result => result.StartedAt).ToArray();
        }

        var artifactsDirectory = GetArtifactsDirectory();
        Directory.CreateDirectory(artifactsDirectory);
        var reportPath = Path.Combine(artifactsDirectory, "youtube-e2e-report.html");
        var passedCount = results.Count(result => result.Status == TestStatus.Passed);
        var failedCount = results.Length - passedCount;
        var totalDuration = results.Sum(result => result.DurationSeconds);
        var resultsMarkup = string.Join(Environment.NewLine, results.Select(RenderCase));

        var html = $$"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="color-scheme" content="light">
              <title>Reporte E2E | Playwright</title>
              <style>
                :root { font-family: "Segoe UI", sans-serif; color: #20272b; background: #f2f4f1; font-synthesis: none; }
                * { box-sizing: border-box; }
                body { margin: 0; min-height: 100vh; background: radial-gradient(ellipse at 82% 0%, #d7eee8 0, transparent 38%), #f2f4f1; }
                main { width: min(940px, calc(100% - 32px)); margin: 0 auto; padding: 48px 0 64px; }
                .topline { display: flex; justify-content: space-between; align-items: center; gap: 16px; color: #56636a; font-size: 13px; letter-spacing: .08em; text-transform: uppercase; }
                .brand { display: inline-flex; align-items: center; gap: 10px; font-weight: 700; }
                .mark { display: grid; place-items: center; width: 30px; height: 30px; border-radius: 9px; color: white; background: #e6443a; font-size: 14px; letter-spacing: 0; }
                h1 { margin: 42px 0 8px; max-width: 700px; font-size: clamp(32px, 6vw, 54px); line-height: 1.04; letter-spacing: -.035em; }
                .subtitle { margin: 0; color: #657177; font-size: 16px; }
                .summary { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin: 32px 0 24px; }
                .metric, .result { border: 1px solid #dfe5e1; border-radius: 8px; background: rgba(255,255,255,.82); }
                .metric { padding: 18px 20px; }
                .metric-label { display: block; margin-bottom: 12px; color: #68757a; font-size: 12px; letter-spacing: .07em; text-transform: uppercase; }
                .metric-value { font-size: 21px; font-weight: 650; }
                .results { display: grid; gap: 14px; }
                .result { overflow: hidden; }
                .result-head { display: flex; justify-content: space-between; align-items: center; gap: 16px; padding: 18px 22px; border-bottom: 1px solid #e7ebe8; }
                h2 { margin: 0; font-size: 17px; letter-spacing: -.01em; }
                .duration { color: #68757a; font-size: 13px; }
                .result-body { padding: 22px; }
                .case-name { margin-bottom: 18px; color: #57646a; font-size: 14px; }
                .status { display: inline-flex; align-items: center; gap: 9px; }
                .dot { width: 10px; height: 10px; border-radius: 50%; background: #14805f; box-shadow: 0 0 0 4px #d8eee5; }
                .status-failed .dot { background: #c43c35; box-shadow: 0 0 0 4px #f8dfdc; }
                .video-label { margin-bottom: 7px; color: #7a8589; font-size: 12px; letter-spacing: .06em; text-transform: uppercase; }
                a { color: #087c68; text-decoration-thickness: 1px; text-underline-offset: 3px; overflow-wrap: anywhere; }
                .evidence-link { display: inline-block; margin-top: 20px; font-size: 14px; font-weight: 600; }
                .muted { color: #8a9498; }
                .error-panel { margin-top: 16px; padding: 18px 22px; border-left: 3px solid #c43c35; border-radius: 4px; background: #fff3f1; }
                .error-panel pre { margin: 12px 0 0; color: #74332e; white-space: pre-wrap; overflow-wrap: anywhere; font: 13px/1.55 ui-monospace, monospace; }
                footer { margin-top: 24px; color: #7a8589; font-size: 12px; }
                @media (max-width: 620px) { main { padding-top: 28px; } .topline { align-items: flex-start; font-size: 11px; } h1 { margin-top: 34px; } .summary { grid-template-columns: 1fr 1fr; } .result-head { align-items: flex-start; flex-direction: column; gap: 7px; } }
              </style>
            </head>
            <body>
              <main>
                <div class="topline"><span class="brand"><span class="mark">▶</span> E2E / Playwright</span><span>Reporte de ejecución</span></div>
                <h1>Pruebas automatizadas</h1>
                <p class="subtitle">Resultados de los casos ejecutados en esta sesión.</p>
                <section class="summary" aria-label="Resumen">
                  <div class="metric"><span class="metric-label">Casos</span><span class="metric-value">{{results.Length}}</span></div>
                  <div class="metric"><span class="metric-label">Aprobados</span><span class="metric-value">{{passedCount}}</span></div>
                  <div class="metric"><span class="metric-label">Con error</span><span class="metric-value">{{failedCount}}</span></div>
                  <div class="metric"><span class="metric-label">Duración total</span><span class="metric-value">{{totalDuration.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}} s</span></div>
                </section>
                <section class="results" aria-label="Casos de prueba">
                  {{resultsMarkup}}
                </section>
                <footer>Generado automáticamente por Microsoft Playwright para .NET.</footer>
              </main>
            </body>
            </html>
            """;

        File.WriteAllText(reportPath, html);
        TestContext.Progress.WriteLine($"Reporte HTML: {reportPath}");
    }

      private static string RenderCase(CaseResult result)
      {
        var passed = result.Status == TestStatus.Passed;
        var statusLabel = passed ? "Correcto" : result.Status.ToString();
        var statusClass = passed ? "status-passed" : "status-failed";
        var screenshotMarkup = result.ScreenshotPath is null
          ? ""
          : $"<a class=\"evidence-link\" href=\"{Escape(Path.GetFileName(result.ScreenshotPath))}\">Ver captura de error</a>";
        var errorMarkup = string.IsNullOrWhiteSpace(result.Message)
          ? ""
          : $"<section class=\"error-panel\"><h2>Detalle del error</h2><pre>{Escape(result.Message)}</pre></section>";
        var targetMarkup = string.IsNullOrWhiteSpace(result.Url)
          ? "<span class=\"muted\">No se obtuvo una página o resultado</span>"
          : $"<a href=\"{Escape(result.Url)}\" target=\"_blank\" rel=\"noreferrer\">{Escape(result.Title ?? result.Url)}</a>";

        return $$"""
          <article class="result">
            <div class="result-head"><h2>{{Escape(result.Name)}}</h2><span class="duration">{{result.DurationSeconds.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}} s</span></div>
            <div class="result-body">
            <div class="case-name"><span class="status {{statusClass}}"><span class="dot"></span>{{statusLabel}}</span></div>
            <div class="video-label">Página o contenido validado</div>
            <div>{{targetMarkup}}</div>
            {{screenshotMarkup}}
            {{errorMarkup}}
            </div>
          </article>
          """;
      }

    private static string Escape(string? value) => WebUtility.HtmlEncode(value ?? "");
}
