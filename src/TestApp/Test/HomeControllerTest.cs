using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Repositories;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.TestApp.Test {
    [TestClass]
    public class TvinstControllerTest {
        internal static HttpClient CreateHttpClient() {
            var args = new string[] { };
            var builder = Program.CreateWebHostBuilder(args);
            var server = new TestServer(builder);
            return server.CreateClient();
        }

        [TestMethod]
        public async Task CanCreateTestClient() {
            var repository = new DvinRepository();
            var dvinApp = await repository.LoadAsync(Constants.DvinSampleAppId);
            Assert.IsNotNull(dvinApp);
            var url = $"http://localhost:{dvinApp.Port}/Home";

            using (var client = CreateHttpClient()) {
                Assert.IsNotNull(client);
                var response = await client.GetAsync(url);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var content = await response.Content.ReadAsStringAsync();
                Assert.IsTrue(content.Contains("Hello World says your dvin app"));
            }
        }
    }
}
