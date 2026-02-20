using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Singletons;
using Microsoft.AspNetCore.Hosting;

// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions;

public static class WebHostBuilderExtensions {
    public static async Task<IWebHostBuilder> UseDvinAndPeghAsync(this IWebHostBuilder builder, string applicationName, string dvinAppId) {
        IDvinApp dvinApp = await DvinAppHelper.LookUpDvinAppAsync(applicationName, dvinAppId);
        builder.UseUrls($"http://localhost:{dvinApp.Port}");
        return builder;
    }
}