using Microsoft.AspNetCore.Mvc;
using WeatherBay.Api.Models;
using WeatherBay.Api.Storage;

namespace WeatherBay.Api.Controllers;

[ApiController]
[Route("api/measurements")]
public sealed class MeasurementsController(MeasurementStore store) : ControllerBase
{
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
