# Pruebas E2E con .NET 10 y Playwright

Proyecto de pruebas de extremo a extremo con NUnit y Microsoft Playwright. La prueba busca un video en YouTube, valida el primer resultado y genera un reporte HTML en `artifacts/youtube-e2e-report.html`.

## Requisitos

- SDK de .NET 10
- PowerShell (`pwsh`), necesario para instalar los navegadores de Playwright

## Preparar el navegador

Con PowerShell:

```bash
dotnet build tests/YoutubeE2E.Tests/YoutubeE2E.Tests.csproj
pwsh tests/YoutubeE2E.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
```

Si no tienes PowerShell, puedes descargar la misma revisión de Chromium con Node.js:

```bash
npx --yes playwright@1.56.0 install chromium
```

En Linux, incluido GitHub Codespaces, si Playwright indica que faltan bibliotecas del sistema, instala las dependencias del navegador:

```bash
sudo npx --yes playwright@1.56.0 install-deps chromium
```

## Ejecutar

```bash
dotnet test tests/YoutubeE2E.Tests/YoutubeE2E.Tests.csproj
```

El reporte se escribe en `artifacts/youtube-e2e-report.html`. Si la prueba falla, también se guarda una captura de pantalla junto al reporte. Para cambiar el texto buscado, define `YOUTUBE_SEARCH_QUERY`; para cambiar la carpeta de artefactos, define `E2E_ARTIFACTS_DIR`.