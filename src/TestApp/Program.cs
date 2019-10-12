using Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.TestApp {
    public class Program {
        public static void Main(string[] args) {
            var builder = CreateWebHostBuilder(args);
            builder.RunHost(args);
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
#if DEBUG
                .UseDvinAndPegh(Constants.DvinSampleAppId, false, args)
#else
                .UseDvinAndPegh(Constants.DvinSampleAppId, true, args)
#endif
                .UseStartup<Startup>();
    }
}
