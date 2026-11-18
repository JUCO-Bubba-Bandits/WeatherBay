using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace WeatherBay.Api.Models;

// Keep posted text until validation so blank and malformed input cannot be confused.
public sealed class ThresholdForm
{
    public string? TemperatureMin { get; set; }
    public string? TemperatureMax { get; set; }
    public string? HumidityMin { get; set; }
    public string? HumidityMax { get; set; }
    public string? WindMin { get; set; }
    public string? WindMax { get; set; }
    public string? RainfallMin { get; set; }
    public string? RainfallMax { get; set; }

    public static ThresholdForm FromSettings(ThresholdSettings settings) => new()
    {
        TemperatureMin = settings.TemperatureMin?.ToString("G", CultureInfo.InvariantCulture),
        TemperatureMax = settings.TemperatureMax?.ToString("G", CultureInfo.InvariantCulture),
        HumidityMin = settings.HumidityMin?.ToString("G", CultureInfo.InvariantCulture),
        HumidityMax = settings.HumidityMax?.ToString("G", CultureInfo.InvariantCulture),
        WindMin = settings.WindMin?.ToString("G", CultureInfo.InvariantCulture),
        WindMax = settings.WindMax?.ToString("G", CultureInfo.InvariantCulture),
        RainfallMin = settings.RainfallMin?.ToString("G", CultureInfo.InvariantCulture),
        RainfallMax = settings.RainfallMax?.ToString("G", CultureInfo.InvariantCulture),
    };

    public bool TryGetSettings(out ThresholdSettings settings, out List<string> errors)
    {
        settings = new();
        var messages = new List<string>();
        double? Parse(string? text, string label)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
                return number;
            messages.Add($"{label} must be a finite number or blank.");
            return null;
        }
        settings.TemperatureMin = Parse(TemperatureMin, "Temperature lower limit");
        settings.TemperatureMax = Parse(TemperatureMax, "Temperature upper limit");
        settings.HumidityMin = Parse(HumidityMin, "Humidity lower limit");
        settings.HumidityMax = Parse(HumidityMax, "Humidity upper limit");
        settings.WindMin = Parse(WindMin, "Wind lower limit");
        settings.WindMax = Parse(WindMax, "Wind upper limit");
        settings.RainfallMin = Parse(RainfallMin, "Rainfall lower limit");
        settings.RainfallMax = Parse(RainfallMax, "Rainfall upper limit");
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(settings, new ValidationContext(settings), results, validateAllProperties: true);
        messages.AddRange(results.Select(x => x.ErrorMessage ?? "Invalid threshold."));
        errors = messages;
        return errors.Count == 0;
    }
}
