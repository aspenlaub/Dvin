using System;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Repositories;
using Microsoft.AspNetCore.Hosting;
// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions {
    public static class WebHostBuilderExtensions {
        public static IWebHostBuilder UseDvin(this IWebHostBuilder builder, string dvinAppId, bool release, string[] mainProgramArgs) {
            var dvinRepository = new DvinRepository();

            var dvinApp = dvinRepository.LoadAsync(dvinAppId).Result;
            if (dvinApp == null) {
                throw new Exception($"Dvin app {dvinAppId} not found");
            }

            builder.UseUrls($"http://localhost:{dvinApp.Port}");
            return builder;
        }

        public static void RunHost(this IWebHostBuilder builder, string[] mainProgramArgs) {
            var host = builder.Build();
            host.Run();
        }
    }
}
