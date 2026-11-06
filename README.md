# WeatherBay

A starter ASP.NET Core Web API targeting .NET 8.

## Current scope

- HTTPS development profile
- `GET /weatherforecast` returns randomly generated sample forecasts, not live weather data
- No database, authentication, or dashboard

## Open and run

Open `WeatherBay.sln` in Visual Studio 2022 with the ASP.NET and web development workload installed. Select the **https** launch profile and press **Ctrl+F5**.

The browser opens `https://localhost:7021/weatherforecast`.

Alternatively, from this directory:

```powershell
dotnet build WeatherBay.sln
dotnet run --project WeatherBay.Api --launch-profile https
```

The repository selects SDK 9.0.316 in `global.json` and targets .NET 8 in the API project. Install a compatible .NET 9 SDK and the .NET 8 ASP.NET Core runtime. The SDK selection avoids an incomplete .NET 8 SDK installation on the original development computer.

If HTTPS needs setup, run `dotnet dev-certs https --trust` and approve the local development certificate prompt.

## Source control

Build output, Visual Studio user files, local configuration, and certificate files are excluded by `.gitignore`. Keep future API keys and other secrets outside tracked files, using development User Secrets or environment variables.
