# WeatherBay

An ASP.NET Core Web API targeting .NET 8.

## Current scope

- HTTPS development profile
- `POST /api/measurements` validates and stores one measurement in SQLite
- Identical retries are acknowledged without inserting another row
- `GET /api/measurements/latest?deviceId=WB-001` reads the newest stored measurement for a device
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

## Local dashboard

Open `https://localhost:7021/dashboard` while the API is running. Select a bay and choose **Show latest** to see its newest saved reading and measurement time in UTC. Bays appear after their first saved reading. Choose **Show latest** again to refresh.

Measurement History charts saved temperatures for the selected bay. Choose **Last 24h**, **7 days**, **1 month**, **6 months**, or **1 year** for a rolling window. For specific calendar days, enter **From** and **Through** dates and select **Apply dates**. Both dates are included in UTC. The chart shows an empty state when the range contains no readings.

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
  "stored": true,
  "duplicate": false,
  "message": "Measurement stored.",
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

A receipt with `stored: true` confirms the measurement is in SQLite. An identical retry returns HTTP 200 with `duplicate: true`. If a submission uses the same device ID and instant with different values, the API returns HTTP 409 and preserves the original reading. Timestamp offsets are normalized for duplicate detection.

The default database is `WeatherBay.Api/App_Data/weatherbay.db` when run from Visual Studio. Set `Storage__DatabasePath` to override the path. Relative paths resolve from the API content root. The default database directory is ignored by Git.

Missing or invalid fields and malformed JSON return **HTTP 400** with standard validation problem details and an `errors` object. Unsupported content types return **HTTP 415**.

## Latest measurement

Send `GET /api/measurements/latest?deviceId=WB-001` to the central API. The response is **HTTP 200** with the same seven measurement fields used by POST, including explicit `null` for an unavailable pH reading. The timestamp is returned in UTC. Readings are ordered by measurement timestamp, so an older upload arriving later does not change the latest result.

A missing or blank `deviceId` returns **HTTP 400**. A device with no saved readings returns **HTTP 404**.

## Measurement history

Send `GET /api/measurements/history?deviceId=WB-001&from=2026-10-26T18:30:00Z&to=2026-10-26T19:00:00Z`. Both bounds require ISO 8601 timestamps with seconds and an explicit UTC offset or `Z`. URL-encode a positive offset's `+` as `%2B`.

The response is **HTTP 200** with an array of readings in ascending measurement time, including the start and excluding the end. An empty range of results returns `[]`, including when the device has no saved readings. Missing parameters, invalid timestamps, and a start at or after the end return **HTTP 400**.

## Devices

Send `GET /api/devices` to populate the dashboard's bay selector. It returns **HTTP 200** with a sorted array of unique device IDs, for example `["WB-001", "WB-002"]`. A bay appears once it has a saved measurement. An empty database returns `[]`.

## Source control

Build output, Visual Studio user files, local configuration, and certificate files are excluded by `.gitignore`. Keep future API keys and other secrets outside tracked files, using development User Secrets or environment variables.
