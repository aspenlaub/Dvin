using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions {
    public static class WebHostBuilderExtensions {
        private static bool IsService(IEnumerable<string> mainProgramArgs) {
            return !(Debugger.IsAttached || mainProgramArgs.Contains("--console"));
        }

        public static void UseAsWindowsService(this IWebHostBuilder builder, string[] mainProgramArgs) {
            if (!IsService(mainProgramArgs)) {
                return;
            }

            var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
            var pathToContentRoot = Path.GetDirectoryName(pathToExe);
            builder.UseContentRoot(pathToContentRoot);
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
