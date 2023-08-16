namespace BuildingBlocks.Jwt;

using BuildingBlocks.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.IdentityModel.Tokens;

public static class JwtExtensions
{
    public static IServiceCollection AddJwt(this IServiceCollection services)
    {
        var jwtOptions = services.GetOptions<JwtBearerOptions>("Jwt");

        services.AddAuthentication(authOptions =>
        {
            authOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            authOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddCookie(cfg => cfg.SlidingExpiration = true)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwtOptions =>
            {
                jwtOptions.Authority = jwtOptions.Authority;
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.FromSeconds(2)
                };
                jwtOptions.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
                jwtOptions.MetadataAddress = jwtOptions.MetadataAddress;
            });

        if (!string.IsNullOrEmpty(jwtOptions.Audience))
        {
            services.AddAuthorization(options =>
                options.AddPolicy(nameof(ApiScope), policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", jwtOptions.Audience);
                })
            );
        }

        return services;
    }
}
