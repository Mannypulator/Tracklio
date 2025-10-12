using Tracklio;
using Tracklio.Shared.Middleware;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.RegisterApplicationServices();
builder.Services.RegisterPersistenceServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.RegisterAppConfigurations(builder.Configuration);
builder.Services.RegisterSwaggerServices();
builder.Services.RegisterJwtServices(builder.Configuration);
builder.Services.RegisterAppConfigurations(builder.Configuration);
builder.Services.RegisterInfrastructureServices();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.RegisterCors();
builder.Services.AddHttpClient();
builder.Services.RegisterFirebase(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Configuration.AddEnvironmentVariables();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

// Add request/response logging
app.UseRequestResponseLogging();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapSliceEndpoints();
app.MapHealthChecks("/health");

// Seed the database
// DbSeeder.Seed(app.Services);

app.Run();
