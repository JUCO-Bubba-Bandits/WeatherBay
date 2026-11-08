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

    public IReadOnlyList<string> GetDeviceIds()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT DeviceId FROM Measurements ORDER BY DeviceId;";
        using var reader = command.ExecuteReader();
        var deviceIds = new List<string>();
        while (reader.Read())
            deviceIds.Add(reader.GetString(0));
        return deviceIds;
    }

    public MeasurementRequest? GetLatest(string deviceId)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DeviceId, TimestampUtcTicks, TemperatureF, HumidityPercent,
                   WindSpeedMph, RainfallRateInHr, RainfallPH
            FROM Measurements
            WHERE DeviceId = $deviceId
            ORDER BY TimestampUtcTicks DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$deviceId", deviceId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadMeasurement(reader) : null;
    }

    public IReadOnlyList<MeasurementRequest> GetHistory(string deviceId, DateTimeOffset from, DateTimeOffset to)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DeviceId, TimestampUtcTicks, TemperatureF, HumidityPercent,
                   WindSpeedMph, RainfallRateInHr, RainfallPH
            FROM Measurements
            WHERE DeviceId = $deviceId
                AND TimestampUtcTicks >= $from
                AND TimestampUtcTicks < $to
            ORDER BY TimestampUtcTicks ASC;
            """;
        command.Parameters.AddWithValue("$deviceId", deviceId);
        command.Parameters.AddWithValue("$from", from.UtcTicks);
        command.Parameters.AddWithValue("$to", to.UtcTicks);
        using var reader = command.ExecuteReader();
        var measurements = new List<MeasurementRequest>();
        while (reader.Read())
            measurements.Add(ReadMeasurement(reader));
        return measurements;
    }

    private static MeasurementRequest ReadMeasurement(SqliteDataReader reader) =>
        new()
        {
            DeviceId = reader.GetString(0),
            Timestamp = new DateTimeOffset(reader.GetInt64(1), TimeSpan.Zero),
            TemperatureF = reader.GetDouble(2),
            HumidityPercent = reader.GetDouble(3),
            WindSpeedMph = reader.GetDouble(4),
            RainfallRateInHr = reader.GetDouble(5),
            RainfallPH = reader.IsDBNull(6) ? null : reader.GetDouble(6)
        };
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
