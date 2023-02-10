﻿using System.Security.Cryptography;
using System.Text;
using CleanMicroserviceSystem.Authentication.Configurations;
using CleanMicroserviceSystem.Authentication.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace CleanMicroserviceSystem.Authentication.Extensions;
public static class CleanMicroserviceSystemAuthenticationExtension
{
    public static IServiceCollection AddJwtBearerAuthentication(
        this IServiceCollection services,
        JwtBearerConfiguration configuration)
    {
        const string UserJwtBearerKey = $"{JwtBearerDefaults.AuthenticationScheme}_User";
        const string ApiJwtBearerKey = $"{JwtBearerDefaults.AuthenticationScheme}_Api";
        services
            .Configure(new Action<JwtBearerConfiguration>(options =>
            {
                options.JwtAudience = configuration.JwtAudience;
                options.JwtExpiryInMinutes = configuration.JwtExpiryInMinutes;
                options.JwtIssuer = configuration.JwtIssuer;
                options.JwtSecurityKey = configuration.JwtSecurityKey;
            }))
            .AddScoped<IJwtBearerTokenGenerator, JwtBearerTokenGenerator>()
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = UserJwtBearerKey;
                options.DefaultChallengeScheme = ApiJwtBearerKey;
            })
            .AddJwtBearer(UserJwtBearerKey, "CleanMicroserviceSystem Bearer for User (IdentityServer)", options =>
            {
                options.Events = new JwtBearerEvents()
                {
                    OnForbidden = context => Task.CompletedTask,
                    OnAuthenticationFailed = context => Task.CompletedTask,
                };
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    // Correction of expiration time's offset
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration.JwtIssuer,
                    ValidAudience = configuration.JwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.JwtSecurityKey))
                };
                options.Validate();
            })
            .AddJwtBearer(ApiJwtBearerKey, "CleanMicroserviceSystem Bearer for Api (IdentityServer)", options =>
            {
                options.Authority = "https://localhost:11002";
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateAudience = false,
                };
            });

        return services;
    }
}
