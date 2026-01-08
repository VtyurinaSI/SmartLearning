using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace UserService
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUserServiceOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<UserRoleOptions>().Bind(configuration.GetSection("UserRoles"));
            return services;
        }

        public static IServiceCollection AddUserServiceCore(this IServiceCollection services)
        {
            services.AddSingleton<IUserContext, UserContext>();
            services.AddScoped<IUserRoleService, UserRoleService>();
            services.AddScoped<UserCreatedHandler>();
            return services;
        }

        public static IServiceCollection AddUserServiceAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
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
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });

            return services;
        }

        public static IServiceCollection AddUserServiceMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddConsumer<UserCreatedConsumer>();
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
    }
}
