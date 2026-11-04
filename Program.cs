using DevToolkit.Auth.Constants;
using DevToolkit.Auth.Extensions;
using DevToolkit.Auth.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Keep this key consistent with appsettings.json and the application's User Secrets.
var connectionString = builder.Configuration.GetConnectionString("YourAppDataBase");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings:YourAppDataBase.");
}

builder.Services.AddAuthentication(AuthSchemes.CustomJwt);
builder.Services.AddDevToolkitAuth(
    builder.Configuration,
    database => database.UseSqlServer(connectionString)
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevToolkitFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (origins.Length > 0)
        {
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("DevToolkitFrontend");
app.UseDevToolkitAuth();
app.MapControllers();
app.MapDevToolkitHealthChecks();

app.Run();
