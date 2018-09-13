using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions {
    public static class WebHostBuilderExtensions {
        private static bool IsService(IEnumerable<string> mainProgramArgs) {
            return !(Debugger.IsAttached || mainProgramArgs.Contains("--console"));
        }

        public static IWebHostBuilder UseDvin(this IWebHostBuilder builder, string dvinAppId, string[] mainProgramArgs) {
            var dvinApp = new DvinRepository().LoadAsync(dvinAppId).Result;
            if (dvinApp == null) {
                throw new Exception($"Dvin app {dvinAppId} not found");
            }

            var port = IsService(mainProgramArgs) ? dvinApp.ReleasePort : dvinApp.DebugPort;
            builder.UseUrls($"http://localhost:{port}");
            if (!IsService(mainProgramArgs)) {
                return builder;
            }

            var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
            var pathToContentRoot = Path.GetDirectoryName(pathToExe);
            builder.UseContentRoot(pathToContentRoot);
            return builder;
        }

        public static void RunHost(this IWebHostBuilder builder, string[] mainProgramArgs) {
            var host = builder.Build();

            if (IsService(mainProgramArgs)) {
                host.RunAsService();
            } else {
                host.Run();
            }
        }
    }
}
