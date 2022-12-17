﻿using System.Reflection;
using CleanMicroserviceSystem.Authentication.Configurations;
using CleanMicroserviceSystem.Authentication.Extensions;
using CleanMicroserviceSystem.Oceanus.Infrastructure.Abstraction.DataSeed;
using CleanMicroserviceSystem.Oceanus.Infrastructure.Abstraction.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using NLog.Web;
using MSLoggingLevel = Microsoft.Extensions.Logging.LogLevel;

namespace CleanMicroserviceSystem.Oceanus.WebAPI.Abstraction;

public class OceanusProgram
{
    protected NLog.ILogger logger;
    protected WebApplicationBuilder webAppBuilder;
    protected WebApplication webApp;
    protected ConfigurationManager configManager;
    protected AssemblyName assemblyName;

    public OceanusProgram(NLog.ILogger logger)
    {
        this.logger = logger;
    }

    public virtual void ConfigureHostBuilder(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        Console.CancelKeyPress += Console_CancelKeyPress;
        webAppBuilder = WebApplication.CreateBuilder(args);
        configManager = webAppBuilder.Configuration;
        webAppBuilder.Logging
            .ClearProviders()
            .AddConsole()
            .AddDebug()
            .SetMinimumLevel(MSLoggingLevel.Debug)
            .AddNLogWeb();
    }

    public virtual void ConfigureServices()
    {
        /* JwtBearerConfiguration was stored in user secrets file
         * Right click on WebAPI project in Solution Explorer window
         * Click Manage User Secrets (User secrets id was specified in <UserSecretsId> of project file)
         * Configurations in user secrets file will be imported into _configurationManager automatically when startup
         */
        var jwtBearerConfiguration = configManager.GetSection("JwtBearerConfiguration").Get<JwtBearerConfiguration>();
        webAppBuilder.Services.AddJwtBearerAuthentication(jwtBearerConfiguration!);
        webAppBuilder.Services.AddHttpContextAccessor();
        webAppBuilder.Services.AddControllers();
        webAppBuilder.Services.AddEndpointsApiExplorer();
        webAppBuilder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(
                JwtBearerDefaults.AuthenticationScheme,
                new OpenApiSecurityScheme()
                {
                    In = ParameterLocation.Header,
                    Description = "Please input token",
                    BearerFormat = "Jwt",
                    Name = "Authorization",
                    Scheme = JwtBearerDefaults.AuthenticationScheme.ToLowerInvariant(),
                    Type = SecuritySchemeType.Http
                });
            options.AddSecurityRequirement(
                new OpenApiSecurityRequirement() {
                    {
                        new OpenApiSecurityScheme() { Reference = new OpenApiReference()
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        } },
                        Array.Empty<string>()
                    }
                });
        });
        webAppBuilder.Services.AddOceanusRepositoryServices();
        webApp = webAppBuilder.Build();
    }

    public virtual void ConfigurePipelines()
    {
        if (webApp.Environment.IsDevelopment())
        {
            webApp.UseSwagger();
            webApp.UseSwaggerUI(options => options.EnablePersistAuthorization());
        }

        webApp.UseWebAPILogging();

        webApp.UseHttpsRedirection();

        webApp.UseAuthorization();

        webApp.MapControllers();
    }

    public virtual void ConfigureWebApp()
    {
        var servicesProvider = webApp.Services;
        servicesProvider.InitializeDatabaseAsync().ConfigureAwait(false);
        var lifetime = servicesProvider.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Register(ApplicationLifetime_ApplicationStarted);
        lifetime.ApplicationStopping.Register(ApplicationLifetime_ApplicationStopping);
        lifetime.ApplicationStopped.Register(ApplicationLifetime_ApplicationStopped);
    }

    public virtual void Run()
    {
        webApp.Run();
    }

    public virtual void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        => logger.Info($"{nameof(Console_CancelKeyPress)} => {(e.Cancel ? "Cancel" : "Not Cancel")}, {e.SpecialKey}");

    public virtual void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        => logger.Info($"{nameof(CurrentDomain_ProcessExit)}");

    public virtual void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        => logger.Error($"{nameof(CurrentDomain_UnhandledException)} => {(e.IsTerminating ? "Terminating" : "Not Terminating")}, {e.ExceptionObject}");

    public virtual void ApplicationLifetime_ApplicationStarted()
        => logger.Info($"{nameof(ApplicationLifetime_ApplicationStarted)}");

    public virtual void ApplicationLifetime_ApplicationStopping()
        => logger.Info($"{nameof(ApplicationLifetime_ApplicationStopping)}");

    public virtual void ApplicationLifetime_ApplicationStopped()
        => logger.Info($"{nameof(ApplicationLifetime_ApplicationStopped)}");
}
