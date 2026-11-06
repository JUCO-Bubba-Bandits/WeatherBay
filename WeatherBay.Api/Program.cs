using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.MapGet("/", () => Results.Ok(new
{
    name = "WeatherBay API",
    measurementEndpoint = "POST /api/measurements",
    storageEnabled = false
}));
app.MapControllers();

app.Run();
