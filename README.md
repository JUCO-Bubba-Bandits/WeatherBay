# WeatherBay

An ASP.NET Core Web API targeting .NET 8.

## Current scope

- HTTPS development profile
- `POST /api/measurements` validates and acknowledges one measurement
- No database, authentication, dashboard, or duplicate protection yet
- The sample `/weatherforecast` endpoint has been removed

## Open and run

Open `WeatherBay.sln` in Visual Studio 2022 with the ASP.NET and web development workload installed. Select **https** and press **Ctrl+F5**.

The browser opens `https://localhost:7021/`, which identifies the API and its measurement endpoint. Sending measurements requires a POST request, not a browser address-bar GET. Open `WeatherBay.Api/WeatherBay.Api.http` in Visual Studio and select **Send Request** above an example.

Alternatively, from this directory:

```powershell
dotnet build WeatherBay.sln
dotnet run --project WeatherBay.Api --launch-profile https
```

The repository selects SDK 9.0.316 in `global.json` and targets .NET 8. Install a compatible .NET 9 SDK and the .NET 8 ASP.NET Core runtime. This SDK selection avoids an incomplete .NET 8 SDK installation on the original development computer.

If HTTPS needs setup, run `dotnet dev-certs https --trust` and approve the local development certificate prompt.

## Measurement request

Send `POST /api/measurements` with `Content-Type: application/json`.

```json
{
  "deviceId": "WB-001",
  "timestamp": "2026-10-26T18:30:00Z",
  "temperatureF": 78.5,
  "humidityPercent": 62.3,
  "windSpeedMph": 8.2,
  "rainfallRateInHr": 0.15,
  "rainfallPH": 6.4
}
```

All seven properties must be present with these exact names and capitalization. Unknown properties are rejected to catch schema mistakes. Numbers must be JSON numbers, not quoted strings.

| Field | Validation |
| --- | --- |
| `deviceId` | Nonempty, not whitespace. Device registration is not checked yet. |
| `timestamp` | ISO 8601 date-time, not the default date-time. Devices should send UTC with a `Z` suffix. Delayed measurements are allowed. |
| `temperatureF` | Finite number in degrees Fahrenheit; no device-specific temperature limits imposed. |
| `humidityPercent` | Number from 0 through 100. |
| `windSpeedMph` | Nonnegative finite number in miles per hour. |
| `rainfallRateInHr` | Nonnegative finite number in inches per hour. |
| `rainfallPH` | Number from 0 through 14, or explicit `null` when no valid pH reading is available. |

A dry interval normally has `"rainfallRateInHr": 0` and `"rainfallPH": null`. A valid pH reading from collected water is still accepted when the rainfall rate is zero.

## Receipt and errors

A valid request returns **HTTP 200**:

```json
{
  "status": "received",
  "stored": false,
  "message": "Measurement validated and received only. It has not been stored; retain the device's local copy.",
  "measurement": {
    "deviceId": "WB-001",
    "timestamp": "2026-10-26T18:30:00+00:00",
    "temperatureF": 78.5,
    "humidityPercent": 62.3,
    "windSpeedMph": 8.2,
    "rainfallRateInHr": 0.15,
    "rainfallPH": 6.4
  }
}
```

The measurement timestamp represents the same instant; UTC may serialize as `+00:00` instead of `Z`. An unavailable pH is echoed as `null`.

**This receipt does not confirm durable storage.** Keep buffered measurements on the device. Submissions are not persisted and repeat submissions are not deduplicated at this stage.

Missing or invalid fields and malformed JSON return **HTTP 400** with standard validation problem details and an `errors` object. Unsupported content types return **HTTP 415**.

## Source control

Build output, Visual Studio user files, local configuration, and certificate files are excluded by `.gitignore`. Keep future API keys and other secrets outside tracked files, using development User Secrets or environment variables.
