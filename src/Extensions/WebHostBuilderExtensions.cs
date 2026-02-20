using System;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Components;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Entities;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Extensions;
using Autofac;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions;

public static class WebHostBuilderExtensions {
    public static async Task<IWebHostBuilder> UseDvinAndPeghAsync(this IWebHostBuilder builder, string applicationName, string dvinAppId) {
        ContainerBuilder containerBuilder = new ContainerBuilder().UseDvinAndPegh(applicationName);
        IContainer container = containerBuilder.Build();
        IDvinRepository dvinRepository = container.Resolve<IDvinRepository>();

        var errorsAndInfos = new ErrorsAndInfos();
        DvinApp dvinApp = await dvinRepository.LoadAsync(dvinAppId, errorsAndInfos)
            ?? throw new Exception($"Dvin app {dvinAppId} not found");

        if (errorsAndInfos.AnyErrors()) {
            throw new Exception(errorsAndInfos.ErrorsToString());
        }

        builder.UseUrls($"http://localhost:{dvinApp.Port}");
        return builder;
    }

    public static void RunHost(this IHostBuilder builder) {
        IHost host = builder.Build();
        host.Run();
    }
}