using Microsoft.AspNetCore.Mvc;
using WeatherBay.Api.Models;

namespace WeatherBay.Api.Controllers;

[ApiController]
[Route("api/measurements")]
public sealed class MeasurementsController : ControllerBase
{
    [HttpPost]
    public IActionResult Receive([FromBody] MeasurementRequest measurement)
    {
        // A receipt confirms validation only; durable storage is a later stage.
        return Ok(new
        {
            status = "received",
            stored = false,
            message = "Measurement validated and received only. It has not been stored; retain the device's local copy.",
            measurement
        });
    }
}
