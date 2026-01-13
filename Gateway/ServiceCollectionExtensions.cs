using Gateway.SrvApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace Gateway
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGatewayOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<DownstreamOptions>().Bind(configuration.GetSection("Downstream"));
            services.AddOptions<JwtOptions>().Bind(configuration.GetSection("Jwt"));
            services.AddOptions<GatewayTimeoutOptions>().Bind(configuration.GetSection("Gateway"));
            return services;
        }

        public static IServiceCollection AddGatewaySwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                var jwtScheme = new OpenApiSecurityScheme
                {
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };

                c.AddSecurityDefinition(jwtScheme.Reference.Id, jwtScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtScheme, Array.Empty<string>() }
                });
            });
            return services;
        }

        public static IServiceCollection AddGatewayHeaderPropagation(this IServiceCollection services)
        {
            services.AddHeaderPropagation(o =>
            {
                o.Headers.Add("Authorization");
                o.Headers.Add("X-Correlation-Id");
                o.Headers.Add("X-User-Id");
            });
            return services;
        }

        public static IServiceCollection AddGatewayHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            var downstream = configuration.GetSection("Downstream").Get<DownstreamOptions>() ?? new DownstreamOptions();
            var gateway = configuration.GetSection("Gateway").Get<GatewayTimeoutOptions>() ?? new GatewayTimeoutOptions();

            services.AddHttpClient<UsersApi>(c =>
                c.BaseAddress = new Uri(downstream.Users))
                .AddHeaderPropagation();
            services.AddHttpClient<ProgressApi>(c =>
                c.BaseAddress = new Uri(downstream.Progress))
                .AddHeaderPropagation();

            services.AddHttpClient<OrchApi>(c =>
            {
                c.BaseAddress = new Uri(downstream.Orch);
                c.Timeout = TimeSpan.FromMinutes(gateway.OrchTimeoutMinutes);
            }).AddHeaderPropagation();

            services.AddHttpClient<AuthApi>(c =>
                c.BaseAddress = new Uri(downstream.Auth))
                .AddHeaderPropagation();
            services.AddHttpClient<PatternsApi>(c =>
                c.BaseAddress = new Uri(downstream.Patterns))
                .AddHeaderPropagation();
            services.AddHttpClient<GatewayObjectStorageClient>(c =>
                c.BaseAddress = new Uri(downstream.Storage));

            return services;
        }

        public static IServiceCollection AddGatewayHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var downstream = configuration.GetSection("Downstream").Get<DownstreamOptions>() ?? new DownstreamOptions();

            services.AddHealthChecks()
                .AddCheck("gateway_self", () => HealthCheckResult.Healthy("OK"))
                .AddUrlGroup(new Uri($"{downstream.Users}health/ready"), name: "users_svc")
                .AddUrlGroup(new Uri($"{downstream.Llm}health/ready"), name: "llm_svc");

            return services;
        }

        public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwt = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
            if (string.IsNullOrWhiteSpace(jwt.Key))
                throw new InvalidOperationException("Jwt:Key РЅРµ Р·Р°РґР°РЅ РІ РєРѕРЅС„РёРіРµ РіРµР№С‚Р°.");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.RequireHttpsMetadata = false;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = false,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                        ValidateLifetime = true,
#warning "ClockSkew СЃ СЂРµС„СЂРµС€РµРј"
                        ClockSkew = TimeSpan.FromMinutes(10)
                    };
                });

            services.AddAuthorization();
            return services;
        }

        public static IServiceCollection AddGatewayLogging(this IServiceCollection services)
        {
            services.AddHttpLogging(o => o.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All);
            return services;
        }
    }
}

