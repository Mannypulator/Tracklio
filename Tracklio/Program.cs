using Tracklio;
using Tracklio.Shared.Slices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.RegisterApplicationServices(builder.Configuration);
builder.Services.RegisterPersistenceServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.RegisterAppConfigurations(builder.Configuration);
builder.Services.RegisterSwaggerServices();
builder.Services.RegisterJwtServices(builder.Configuration);
builder.Services.RegisterAppConfigurations(builder.Configuration);
builder.Services.RegisterInfrastructureServices();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapSliceEndpoints();
app.MapHealthChecks("/health");


app.Run();
