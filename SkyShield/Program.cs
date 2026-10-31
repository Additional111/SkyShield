using SkyShield.Services.Ingestion;

var builder = WebApplication.CreateBuilder(args);

// Configurăm adresa serverului.
builder.WebHost.UseUrls("http://localhost:5000");

// Înregistrăm serviciile aplicației.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Înregistrăm serviciile proprii.
builder.Services.AddScoped<AircraftValidationService>();

var app = builder.Build();

// Configurăm OpenAPI.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Permitem servirea dashboard-ului din wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();