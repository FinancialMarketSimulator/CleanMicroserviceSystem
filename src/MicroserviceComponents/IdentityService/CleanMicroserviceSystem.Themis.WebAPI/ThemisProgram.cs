﻿using CleanMicroserviceSystem.Oceanus.Application.Abstraction.Configurations;
using CleanMicroserviceSystem.Oceanus.Infrastructure.Abstraction.DataSeed;
using CleanMicroserviceSystem.Oceanus.WebAPI.Abstraction;
using CleanMicroserviceSystem.Themis.Infrastructure;
using CleanMicroserviceSystem.Themis.Infrastructure.DataSeeds;
using IdentityServer4.EntityFramework.DbContexts;

namespace CleanMicroserviceSystem.Themis.WebAPI;

public class ThemisProgram : OceanusProgram
{
    public ThemisProgram(NLog.ILogger logger) : base(logger) { }

    public override void ConfigureServices()
    {
        webAppBuilder.Services.AddInfrastructure(new OceanusDbConfiguration()
        {
            ConnectionString = configManager.GetConnectionString("ServiceDB")!
        });
        base.ConfigureServices();
    }

    public override void ConfigurePipelinesBeforeAuth()
    {
        webApp.UseIdentityServer();
        base.ConfigurePipelinesBeforeAuth();
    }

    public override void ConfigureWebApp()
    {
        base.ConfigureWebApp();

        var servicesProvider = webApp.Services;
        servicesProvider.InitializeDatabaseAsync<PersistedGrantDbContext>().ConfigureAwait(false);
        servicesProvider.InitializeDatabaseAsync<ConfigurationDbContext>().ConfigureAwait(false);
        servicesProvider.InitializeConfigurationDataAsync().ConfigureAwait(false);
    }
}
