using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FirebaseAdmin;
using FluentValidation;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Tracklio.Shared.Behaviours;
using Tracklio.Shared.Configurations;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Metrics;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Security;
using Tracklio.Shared.Services;
using Tracklio.Shared.Services.Notification;
using Tracklio.Shared.Services.Otp;
using Tracklio.Shared.Services.Token;
using Tracklio.Shared.Slices;

namespace Tracklio;

public static class ServiceCollectionExtension
{
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterSlices();
        var currentAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(currentAssembly)
            .RegisterServicesFromAssemblies(currentAssembly)
                .AddOpenRequestPreProcessor(typeof(LoggingBehaviour<>))
                .AddOpenBehavior(typeof(ModelValidationBehaviour<,>))
                .AddOpenBehavior(typeof(HandlerPerformanceMetricBehaviour<,>));
        });
        services.AddValidatorsFromAssembly(currentAssembly);
        services.AddSingleton<HandlerPerformanceMetric>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHealthChecks()
            .AddDbContextCheck<RepositoryContext>()
            .AddNpgSql(configuration.GetConnectionString("TracklioDbConnection")!)
            .AddCheck("api-health", () => HealthCheckResult.Healthy("API is running"));
            
        return services;
    }

    public static IServiceCollection RegisterPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<RepositoryContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("TracklioDbConnection")));
        return services;
    }
    
    public static IServiceCollection RegisterJwtServices(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"]!))
                };
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                var g = configuration.GetSection("Authentication:Google");
                options.ClientId     = g["ClientId"]!;
                options.ClientSecret = g["ClientSecret"]!;
                options.CallbackPath = g["CallbackPath"]!;
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Surname,   "family_name");
                options.ClaimActions.MapJsonKey("picture", "picture", "url");
            });
        
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PoliciesConstant.AdminOnly, policy => policy.RequireRole(nameof(UserRole.Admin)));
            options.AddPolicy(PoliciesConstant.MotoristOrAdmin, policy => policy.RequireRole(nameof(UserRole.Motorist),nameof(UserRole.Admin)));
            options.AddPolicy(PoliciesConstant.MotoristOnly, policy => policy.RequireRole( nameof(UserRole.Motorist)));
        });

        return services;
    }
    
    public static IServiceCollection RegisterSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tracklio API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter JWT with Bearer into field (e.g., Bearer {token})",
                Name = "Authorization",
                Type = SecuritySchemeType.Http, 
                Scheme = "bearer", 
                BearerFormat = "JWT" 
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    [] 
                }
            });
        });

        return services;
    }
    
    public static IServiceCollection RegisterAppConfigurations(this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var smtpSettings = configuration.GetSection("SmtpSettings");
        var authenticationSettings = configuration.GetSection("Authentication");
        services.Configure<JwtSettings>(jwtSettings);
        services.Configure<SmtpSettings>(smtpSettings);
        services.Configure<Authentication>(authenticationSettings);


        return services;
    }

    public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IOtpService, OtpService>();
        return services;
    }

    public static IServiceCollection RegisterCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigins", policy =>
            {
                policy.WithOrigins("https://localhost:3000", "https://yourdomain.com") 
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); 
            });
            
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        
        return services;
    }

    public static IServiceCollection RegisterFirebase(this IServiceCollection services, IConfiguration configuration)
    {
        var firebaseConfig = configuration.GetSection("Firebase");
        var serviceAccountKey = firebaseConfig.GetSection("ServiceAccountKey").Get<Dictionary<string, object>>();
        if (serviceAccountKey != null)
        {
            var keyJson = JsonSerializer.Serialize(serviceAccountKey);
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromJson(keyJson),
                ProjectId = firebaseConfig["ProjectId"]
            });
        }
        else
        {
            throw new InvalidOperationException("Firebase service account key not configured properly");
        }

        services.AddScoped<IFirebaseService, FirebaseService>();
        return services;
        
    }

}
