using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace WeatherBay.Api.Models;

public sealed class ThresholdSettings : IValidatableObject
{
    public double? TemperatureMin { get; set; }
    public double? TemperatureMax { get; set; }
    public double? HumidityMin { get; set; }
    public double? HumidityMax { get; set; }
    public double? WindMin { get; set; }
    public double? WindMax { get; set; }
    public double? RainfallMin { get; set; }
    public double? RainfallMax { get; set; }

    public bool HasLimits => Limits().Any(x => x.Min.HasValue || x.Max.HasValue);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var limit in Limits())
        {
            foreach (var (value, member) in new[] { (limit.Min, limit.MinMember), (limit.Max, limit.MaxMember) })
            {
                if (value is double number && (!double.IsFinite(number) ||
                    (limit.Floor is double floor && number < floor) ||
                    (limit.Ceiling is double ceiling && number > ceiling)))
                    yield return new($"{limit.Label} limits must be finite numbers{limit.Requirement}.", [member]);
            }
            if (limit.Min.HasValue && limit.Max.HasValue && limit.Min > limit.Max)
                yield return new($"{limit.Label} lower limit must be less than or equal to its upper limit.",
                    [limit.MinMember, limit.MaxMember]);
        }
    }

    public IReadOnlyList<string> Evaluate(MeasurementRequest reading)
    {
        var values = new[] { reading.TemperatureF, reading.HumidityPercent, reading.WindSpeedMph, reading.RainfallRateInHr };
        var warnings = new List<string>();
        var index = 0;
        foreach (var limit in Limits())
        {
            var value = values[index++];
            if (limit.Min is double min && value < min)
                warnings.Add($"{limit.Label} {Format(value)} {limit.Unit} is below the lower limit of {Format(min)} {limit.Unit}.");
            else if (limit.Max is double max && value > max)
                warnings.Add($"{limit.Label} {Format(value)} {limit.Unit} is above the upper limit of {Format(max)} {limit.Unit}.");
        }
        return warnings;
    }

    private static string Format(double value) => value.ToString("G", CultureInfo.InvariantCulture);
    private IEnumerable<Limit> Limits()
    {
        yield return new("Temperature", "°F", TemperatureMin, TemperatureMax, nameof(TemperatureMin), nameof(TemperatureMax), null, null, "");
        yield return new("Humidity", "%", HumidityMin, HumidityMax, nameof(HumidityMin), nameof(HumidityMax), 0, 100, " from 0 through 100");
        yield return new("Wind speed", "mph", WindMin, WindMax, nameof(WindMin), nameof(WindMax), 0, null, " at or above 0");
        yield return new("Rainfall rate", "in/hr", RainfallMin, RainfallMax, nameof(RainfallMin), nameof(RainfallMax), 0, null, " at or above 0");
    }
    private sealed record Limit(string Label, string Unit, double? Min, double? Max,
        string MinMember, string MaxMember, double? Floor, double? Ceiling, string Requirement);
}
