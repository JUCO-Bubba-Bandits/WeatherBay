using Microsoft.Data.Sqlite;
using WeatherBay.Api.Models;

namespace WeatherBay.Api.Storage;

public enum MeasurementSaveResult
{
    Inserted,
    Duplicate,
    Conflict
}

public sealed class MeasurementStore(string databasePath)
{
    private readonly string connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath
    }.ToString();

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Measurements (
                DeviceId TEXT NOT NULL,
                TimestampUtcTicks INTEGER NOT NULL,
                TemperatureF REAL NOT NULL,
                HumidityPercent REAL NOT NULL,
                WindSpeedMph REAL NOT NULL,
                RainfallRateInHr REAL NOT NULL,
                RainfallPH REAL,
                PRIMARY KEY (DeviceId, TimestampUtcTicks)
            );
            """;
        command.ExecuteNonQuery();
    }

    public MeasurementSaveResult Save(MeasurementRequest measurement)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.Parameters.AddWithValue("$deviceId", measurement.DeviceId);
        command.Parameters.AddWithValue("$timestamp", measurement.Timestamp.UtcTicks);
        command.Parameters.AddWithValue("$temperature", measurement.TemperatureF);
        command.Parameters.AddWithValue("$humidity", measurement.HumidityPercent);
        command.Parameters.AddWithValue("$wind", measurement.WindSpeedMph);
        command.Parameters.AddWithValue("$rainfall", measurement.RainfallRateInHr);
        command.Parameters.AddWithValue("$ph", (object?)measurement.RainfallPH ?? DBNull.Value);
        command.CommandText = """
            INSERT INTO Measurements (
                DeviceId, TimestampUtcTicks, TemperatureF, HumidityPercent,
                WindSpeedMph, RainfallRateInHr, RainfallPH
            ) VALUES ($deviceId, $timestamp, $temperature, $humidity, $wind, $rainfall, $ph)
            ON CONFLICT (DeviceId, TimestampUtcTicks) DO NOTHING;
            """;
        if (command.ExecuteNonQuery() == 1)
            return MeasurementSaveResult.Inserted;

        // Stored measurements are immutable, so a concurrent retry cannot change this comparison.
        command.CommandText = """
            SELECT 1 FROM Measurements
            WHERE DeviceId = $deviceId AND TimestampUtcTicks = $timestamp
                AND TemperatureF = $temperature AND HumidityPercent = $humidity
                AND WindSpeedMph = $wind AND RainfallRateInHr = $rainfall
                AND RainfallPH IS $ph;
            """;
        return command.ExecuteScalar() is null
            ? MeasurementSaveResult.Conflict
            : MeasurementSaveResult.Duplicate;
    }
}
