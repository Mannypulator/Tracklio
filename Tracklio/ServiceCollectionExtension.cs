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
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
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

        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        Console.WriteLine($"[DEBUG] CONNECTION_STRING = {connectionString}");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("CONNECTION_STRING environment variable is missing.");

        services.AddHealthChecks()
            .AddDbContextCheck<RepositoryContext>()
            .AddNpgSql(connectionString)
            .AddCheck("api-health", () => HealthCheckResult.Healthy("API is running"));

        return services;
    }


    public static IServiceCollection RegisterPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var isDevelopment = configuration.GetValue<bool>("ApplicationSettings:IsDevelopment");
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (isDevelopment)
            {
                connectionString = configuration.GetConnectionString("TracklioDbConnection");
            }

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Database connection string is missing. Check environment variables or appsettings.");
        }

        services.AddDbContext<RepositoryContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }



    public static IServiceCollection RegisterJwtServices(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

        if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience) ||
            string.IsNullOrWhiteSpace(jwtSecretKey))
        {
            throw new InvalidOperationException("Missing JWT environment variables");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
                };
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
                var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
                var googleCallbackPath = Environment.GetEnvironmentVariable("GOOGLE_CALLBACK_PATH");

                if (string.IsNullOrWhiteSpace(googleClientId) || string.IsNullOrWhiteSpace(googleClientSecret) ||
                    string.IsNullOrWhiteSpace(googleCallbackPath))
                    throw new InvalidOperationException("Missing Google authentication environment variables");

                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.CallbackPath = googleCallbackPath;

                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
                options.ClaimActions.MapJsonKey("picture", "picture", "url");
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PoliciesConstant.AdminOnly, policy => policy.RequireRole(nameof(UserRole.Admin)));
            options.AddPolicy(PoliciesConstant.MotoristOrAdmin,
                policy => policy.RequireRole(nameof(UserRole.Motorist), nameof(UserRole.Admin)));
            options.AddPolicy(PoliciesConstant.MotoristOnly, policy => policy.RequireRole(nameof(UserRole.Motorist)));
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

    public static IServiceCollection RegisterAppConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = new JwtSettings
        {
            Issuer    = Environment.GetEnvironmentVariable("JWT_ISSUER")!,
            Audience  = Environment.GetEnvironmentVariable("JWT_AUDIENCE")!,
            SecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")!,
            ExpireMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES")!)
        };

        var smtpSettings = new SmtpSettings
        {
            UserName     = Environment.GetEnvironmentVariable("SMTP_USERNAME")!,
            Password     = Environment.GetEnvironmentVariable("SMTP_PASSWORD")!,
            DisplayName  = Environment.GetEnvironmentVariable("SMTP_DISPLAY_NAME")!,
            Server       = Environment.GetEnvironmentVariable("SMTP_SERVER")!,
            Port         = Environment.GetEnvironmentVariable("SMTP_PORT")!
        };

        var authentication = new Authentication
        {
            Google = new Shared.Configurations.Google
            {
                ClientId     = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")!,
                ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")!,
                CallbackPath = Environment.GetEnvironmentVariable("GOOGLE_CALLBACK_PATH")!
            }
        };

        services.Configure<JwtSettings>(_ => { _.Issuer = jwtSettings.Issuer; _.Audience = jwtSettings.Audience; _.SecretKey = jwtSettings.SecretKey;
            _.ExpireMinutes = jwtSettings.ExpireMinutes;
        });
        services.Configure<SmtpSettings>(_ => {
            _.UserName    = smtpSettings.UserName;
            _.Password    = smtpSettings.Password;
            _.DisplayName = smtpSettings.DisplayName;
            _.Server      = smtpSettings.Server;
            _.Port        = smtpSettings.Port;
        });
        services.Configure<Authentication>(_ => {
            _.Google = authentication.Google;
        });

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
        var serviceAccountKey = new Dictionary<string, object?>
        {
            { "type", Environment.GetEnvironmentVariable("FIREBASE_TYPE") },
            { "project_id", Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID") },
            { "private_key_id", Environment.GetEnvironmentVariable("FIREBASE_PRIVATE_KEY_ID") },
            { "private_key", Environment.GetEnvironmentVariable("FIREBASE_PRIVATE_KEY")?.Replace("\\n", "\n") },
            { "client_email", Environment.GetEnvironmentVariable("FIREBASE_CLIENT_EMAIL") },
            { "client_id", Environment.GetEnvironmentVariable("FIREBASE_CLIENT_ID") },
            { "auth_uri", Environment.GetEnvironmentVariable("FIREBASE_AUTH_URI") },
            { "token_uri", Environment.GetEnvironmentVariable("FIREBASE_TOKEN_URI") },
            {
                "auth_provider_x509_cert_url",
                Environment.GetEnvironmentVariable("FIREBASE_AUTH_PROVIDER_X509_CERT_CURL")
            },
            { "client_x509_cert_url", Environment.GetEnvironmentVariable("FIREBASE_CLIENT_X509_CERT_CURL") },
            { "universe_domain", Environment.GetEnvironmentVariable("FIREBASE_UNIVERSE_DOMAIN") }
        };

        // Optional: Validate required fields
        if (serviceAccountKey.Values.Any(v => string.IsNullOrWhiteSpace(v as string)))
            throw new InvalidOperationException("Missing one or more Firebase environment variables.");


        var keyJson = JsonSerializer.Serialize(serviceAccountKey);

        FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromJson(keyJson),
            ProjectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")
        });

        services.AddScoped<IFirebaseService, FirebaseService>();
        return services;
    }
}