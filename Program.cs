using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PerformanceApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Controllers. Enums are sent/received as text ("Draft"), not numbers.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// CORS: lets the React app (port 5173) call this API.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// One database for the whole project. The connection string lives in
// appsettings.Development.json (never committed).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");   // must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();
