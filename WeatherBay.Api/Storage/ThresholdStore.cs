using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using WeatherBay.Api.Models;

namespace WeatherBay.Api.Storage;

public sealed class ThresholdStore(string databasePath)
{
    private readonly string connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();

    public void Initialize()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS DeviceThresholds (
                DeviceId TEXT PRIMARY KEY NOT NULL,
                SettingsJson TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public ThresholdSettings Get(string deviceId)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT SettingsJson FROM DeviceThresholds WHERE DeviceId = $deviceId;";
        command.Parameters.AddWithValue("$deviceId", deviceId);
        return command.ExecuteScalar() is string json
            ? JsonSerializer.Deserialize<ThresholdSettings>(json) ?? new()
            : new();
    }

    public void Save(string deviceId, ThresholdSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        Validator.ValidateObject(settings, new ValidationContext(settings), validateAllProperties: true);
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO DeviceThresholds (DeviceId, SettingsJson) VALUES ($deviceId, $settings)
            ON CONFLICT(DeviceId) DO UPDATE SET SettingsJson = excluded.SettingsJson;
            """;
        command.Parameters.AddWithValue("$deviceId", deviceId);
        command.Parameters.AddWithValue("$settings", JsonSerializer.Serialize(settings));
        command.ExecuteNonQuery();
    }
}
