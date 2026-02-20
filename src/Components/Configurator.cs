using System;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Entities;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Extensions;
using Autofac;
using Microsoft.AspNetCore.Hosting;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Components;

public static class Configurator {
    public static IWebHostBuilder ConfigureUrl(this IWebHostBuilder builder, string applicationName, string dvinAppId) {
        return ConfigureUrlAsync(builder, applicationName, dvinAppId).Result;
    }

    public static async Task<IWebHostBuilder> ConfigureUrlAsync(this IWebHostBuilder builder, string applicationName, string dvinAppId) {
        IDvinApp dvinApp = await LookUpDvinAppAsync(applicationName, dvinAppId);
        builder.UseUrls($"http://localhost:{dvinApp.Port}");
        return builder;
    }

    private static async Task<IDvinApp> LookUpDvinAppAsync(string applicationName, string dvinAppId) {
        ContainerBuilder containerBuilder = new ContainerBuilder().UseDvinAndPegh(applicationName);
        IContainer container = containerBuilder.Build();
        IDvinRepository dvinRepository = container.Resolve<IDvinRepository>();

        var errorsAndInfos = new ErrorsAndInfos();
        DvinApp dvinApp = await dvinRepository.LoadAsync(dvinAppId, errorsAndInfos)
            ?? throw new Exception($"Dvin app {dvinAppId} not found");

        return errorsAndInfos.AnyErrors()
            ? throw new Exception(errorsAndInfos.ErrorsToString())
            : dvinApp;
    }
}
