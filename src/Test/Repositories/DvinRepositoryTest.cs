using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Components;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Repositories {
    [TestClass]
    public class DvinRepositoryTest {
        private readonly IContainer vContainer;

        public DvinRepositoryTest() {
            var csArgumentPrompterMock = new Mock<ICsArgumentPrompter>();
            var builder = new ContainerBuilder().RegisterForPegh(csArgumentPrompterMock.Object).RegisterForDvin();
            vContainer = builder.Build();
        }

        [TestMethod]
        public async Task CanGetDvinApps() {
            var sut = vContainer.Resolve<IDvinRepository>();
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
            var sut = vContainer.Resolve<IDvinRepository>();
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
