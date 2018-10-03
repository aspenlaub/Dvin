using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Repositories;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.TestApp.Test {
    [TestClass]
    public class TvinstControllerTest {
        [TestMethod]
        public async Task CanCreateTestClient() {
            var dvinApp = await GetDvinApp();
            var url = $"http://localhost:{dvinApp.Port}/Home";

            using (var client = CreateHttpClient()) {
                Assert.IsNotNull(client);
                var response = await client.GetAsync(url);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var content = await response.Content.ReadAsStringAsync();
                Assert.IsTrue(content.Contains("Hello World says your dvin app"));
            }
        }

        [TestMethod]
        public async Task CanHandleCrashes() {
            var dvinApp = await GetDvinApp();
            var url = $"http://localhost:{dvinApp.Port}/Home/Crash";

            foreach (var file in FilesWithDeliberateExceptionLogged(dvinApp)) {
                File.Delete(file);
            }

            using (var client = CreateHttpClient()) {
                Assert.IsNotNull(client);
                var response = await client.GetAsync(url);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var content = await response.Content.ReadAsStringAsync();
                Assert.IsTrue(content.Contains("An exception was logged"));
            }

            var files = FilesWithDeliberateExceptionLogged(dvinApp);
            Assert.AreEqual(1, files.Count);
            foreach (var file in files) {
                File.Delete(file);
            }
        }

        internal static HttpClient CreateHttpClient() {
            var args = new string[] { };
            var builder = Program.CreateWebHostBuilder(args);
            var server = new TestServer(builder);
            return server.CreateClient();
        }

        private static async Task<DvinApp> GetDvinApp() {
            var repository = new DvinRepository();
            var dvinApp = await repository.LoadAsync(Constants.DvinSampleAppId);
            Assert.IsNotNull(dvinApp);
            return dvinApp;
        }

        private static IList<string> FilesWithDeliberateExceptionLogged(IDvinApp dvinApp) {
            var dvinAppFolder = dvinApp.FolderOnMachine(Environment.MachineName);
            if (dvinAppFolder == null) {
                throw new Exception($"Folders for dvin app {dvinApp.Id} not found");
            }

            return Directory.GetFiles(dvinAppFolder.ExceptionLogFolder, "Ex*.txt", SearchOption.TopDirectoryOnly)
                .Where(f => File.ReadAllText(f).Contains("This is a deliberate crash")).ToList();
        }
    }
}
