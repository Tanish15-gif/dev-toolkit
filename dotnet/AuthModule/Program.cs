using AuthModule.Extensions;
using Module.Data; // swap for your new project's DbContext namespace

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// ---- Project-specific: the only DB line you need to write ----
builder.Services.AddDbContext<ModuleDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- One line replaces the entire ~200-line auth block ----
builder.Services.AddJwtAuthModule(builder.Configuration);

// ---- Health checks: project's own + the auth module's ----
builder.Services.AddHealthChecks()
    .AddAuthModuleHealthChecks();
// .AddCheck<...>(...) — add your own project-specific checks here

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Order matters: Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();