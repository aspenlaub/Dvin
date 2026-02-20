using System;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Components;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Entities;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Extensions;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Singletons;

public static class DvinAppHelper {
    public static IDvinApp LookUpDvinApp(string applicationName, string dvinAppId) {
        return LookUpDvinAppAsync(applicationName, dvinAppId).Result;
    }

    public static async Task<IDvinApp> LookUpDvinAppAsync(string applicationName, string dvinAppId) {
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
