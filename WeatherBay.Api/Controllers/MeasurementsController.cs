using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WeatherBay.Api.Models;
using WeatherBay.Api.Storage;

namespace WeatherBay.Api.Controllers;

[ApiController]
[Route("api/measurements")]
public sealed class MeasurementsController(MeasurementStore store) : ControllerBase
{
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
