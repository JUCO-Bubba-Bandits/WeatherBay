using System.Text.Json.Serialization;
using WeatherBay.Api.Storage;

var builder = WebApplication.CreateBuilder(args);
var databasePath = Path.GetFullPath(
    builder.Configuration["Storage:DatabasePath"] ?? "App_Data/weatherbay.db",
    builder.Environment.ContentRootPath);
builder.Services.AddSingleton(new MeasurementStore(databasePath));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});

var app = builder.Build();
app.Services.GetRequiredService<MeasurementStore>().Initialize();

app.UseHttpsRedirection();
app.MapGet("/", () => Results.Ok(new
{
    name = "WeatherBay API",
    measurementEndpoint = "POST /api/measurements",
    storageEnabled = true
}));
app.MapControllers();
app.MapGet("/api/devices", (MeasurementStore store) => Results.Ok(store.GetDeviceIds()));

app.Run();
