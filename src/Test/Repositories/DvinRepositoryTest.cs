using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Repositories;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Repositories {
    [TestClass]
    public class DvinRepositoryTest {
        private readonly IComponentProvider vComponentProvider;

        public DvinRepositoryTest() {
            vComponentProvider = new ComponentProvider();
        }

        [TestMethod]
        public async Task CanGetDvinApps() {
            var sut = new DvinRepository(vComponentProvider);
            var errorsAndInfos = new ErrorsAndInfos();
            var apps = await sut.LoadAsync(errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsTrue(apps.Any());
            foreach (var app in apps.Where(a => !a.Id.Contains("Grasp"))) {
                Assert.IsTrue(Directory.Exists(app.SolutionFolder));
                Assert.IsTrue(Directory.Exists(app.ReleaseFolder));
                Assert.IsTrue(Directory.Exists(app.PublishFolder));
                Assert.IsTrue(Directory.Exists(app.ExceptionLogFolder));
            }
        }

        [TestMethod]
        public async Task CanGetDvinApp() {
            var sut = new DvinRepository(vComponentProvider);
            var errorsAndInfos = new ErrorsAndInfos();
            var apps = await sut.LoadAsync(errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsTrue(apps.Any());
            var app = await sut.LoadAsync(apps[0].Id, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.AreEqual(app.Id, apps[0].Id);
        }
    }
}
