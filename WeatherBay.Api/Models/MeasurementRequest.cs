using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WeatherBay.Api.Models;

public sealed class MeasurementRequest : IValidatableObject
{
    [Required]
    [JsonPropertyName("deviceId")]
    public required string DeviceId { get; init; }

    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("temperatureF")]
    public required double TemperatureF { get; init; }

    [JsonPropertyName("humidityPercent")]
    public required double HumidityPercent { get; init; }

    [JsonPropertyName("windSpeedMph")]
    public required double WindSpeedMph { get; init; }

    [JsonPropertyName("rainfallRateInHr")]
    public required double RainfallRateInHr { get; init; }

    // Required in JSON, but explicitly null when no valid reading is available.
    [JsonPropertyName("rainfallPH")]
    public required double? RainfallPH { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Timestamp == default)
            yield return new("A non-default timestamp is required.", ["timestamp"]);

        if (!double.IsFinite(TemperatureF))
            yield return new("Temperature must be a finite number in degrees Fahrenheit.", ["temperatureF"]);

        if (!double.IsFinite(HumidityPercent) || HumidityPercent < 0 || HumidityPercent > 100)
            yield return new("Humidity must be between 0 and 100 percent.", ["humidityPercent"]);

        if (!double.IsFinite(WindSpeedMph) || WindSpeedMph < 0)
            yield return new("Wind speed must be a non-negative finite number in mph.", ["windSpeedMph"]);

        if (!double.IsFinite(RainfallRateInHr) || RainfallRateInHr < 0)
            yield return new("Rainfall rate must be a non-negative finite number in inches per hour.", ["rainfallRateInHr"]);

        if (RainfallPH is double ph && (!double.IsFinite(ph) || ph < 0 || ph > 14))
            yield return new("Rainfall pH must be null or a number between 0 and 14.", ["rainfallPH"]);
    }
}
