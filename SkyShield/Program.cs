using SkyShield.Services.Ingestion;

var builder = WebApplication.CreateBuilder(args);

// Configurăm adresa serverului pentru demo.
builder.WebHost.UseUrls("http://localhost:5000");

// Înregistrăm serviciile aplicației.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Înregistrăm serviciile proprii.
builder.Services.AddScoped<AircraftValidationService>();

var app = builder.Build();

// Configurăm pipeline-ul HTTP.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();