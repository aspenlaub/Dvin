using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Repositories {
    [TestClass]
    public class DvinRepositoryTest {
        [TestMethod]
        public async Task CanGetDvinApps() {
            var sut = new DvinRepository();
            var apps = await sut.LoadAsync();
            Assert.IsTrue(apps.Any());
        }

        [TestMethod]
        public async Task CanGetDvinApp() {
            var sut = new DvinRepository();
            var apps = await sut.LoadAsync();
            Assert.IsTrue(apps.Any());
            var app = await sut.LoadAsync(apps[0].Id);
            Assert.AreEqual(app.Id, apps[0].Id);
        }
    }
}
