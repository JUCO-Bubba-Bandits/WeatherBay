using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using WeatherBay.Api.Models;
using WeatherBay.Api.Storage;

namespace WeatherBay.Api.Controllers;

[ApiController]
[Route("api/measurements")]
public sealed class MeasurementsController(MeasurementStore store) : ControllerBase
{
    private static readonly string[] HistoryTimestampFormats =
        ["yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"];

    [HttpGet("history")]
    public ActionResult<IReadOnlyList<MeasurementRequest>> GetHistory(
        [FromQuery, Required] string deviceId,
        [FromQuery, Required] string from,
        [FromQuery, Required] string to)
    {
        if (!DateTimeOffset.TryParseExact(from, HistoryTimestampFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var start) ||
            !DateTimeOffset.TryParseExact(to, HistoryTimestampFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var end))
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                detail: "From and to must be ISO 8601 timestamps with Z or a UTC offset.");

        if (start >= end)
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                detail: "From must be earlier than to.");

        return Ok(store.GetHistory(deviceId, start, end));
    }

    [HttpGet("latest")]
    public ActionResult<MeasurementRequest> GetLatest([FromQuery, Required] string deviceId)
    {
        var measurement = store.GetLatest(deviceId);
        if (measurement is null)
            return Problem(statusCode: StatusCodes.Status404NotFound,
                detail: "No saved measurements were found for this device.");

        return Ok(measurement);
    }

    [HttpPost]
    public IActionResult Receive([FromBody] MeasurementRequest measurement)
    {
        var result = store.Save(measurement);
        if (result == MeasurementSaveResult.Conflict)
            return Problem(statusCode: StatusCodes.Status409Conflict,
                detail: "A different measurement is already stored for this device and timestamp.");

        var duplicate = result == MeasurementSaveResult.Duplicate;
        return Ok(new
        {
            status = "received",
            stored = true,
            duplicate,
            message = duplicate ? "Measurement was already stored." : "Measurement stored.",
            measurement
        });
    }
}
