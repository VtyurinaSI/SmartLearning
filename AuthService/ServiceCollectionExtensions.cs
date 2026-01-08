using AuthService.Data;
using AuthService.Models;
using AuthService.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace AuthService
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthServiceOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<JwtOptions>().Bind(configuration.GetSection("Jwt"));
            services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection("RabbitMq"));
            services.AddOptions<DbMigrationOptions>().Bind(configuration.GetSection("DbMigration"));
            return services;
        }

        public static IServiceCollection AddAuthServiceDb(this IServiceCollection services, IConfiguration configuration)
        {
            var cs = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(cs));
            return services;
        }

        public static IServiceCollection AddAuthServiceMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(options.Host, options.VirtualHost, h =>
                    {
                        h.Username(options.UserName);
                        h.Password(options.Password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }

        public static IServiceCollection AddAuthServiceIdentity(this IServiceCollection services)
        {
            services.AddIdentityCore<User>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            return services;
        }

        public static IServiceCollection AddAuthServiceAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwt = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
            if (string.IsNullOrWhiteSpace(jwt.Key))
            {
                throw new InvalidOperationException("Jwt:Key is not configured");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwt.Key))
                    };
                });

            return services;
        }

        public static IServiceCollection AddAuthServiceSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth Service API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
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
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }

        public static IServiceCollection AddAuthServiceCore(this IServiceCollection services)
        {
            services.AddControllers();
            services.AddScoped<IAuthService, AuthService.Services.AuthService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<AuthDbMigrator>();
            return services;
        }
    }
}
