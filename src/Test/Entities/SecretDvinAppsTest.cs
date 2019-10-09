using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Entities {
    [TestClass]
    public class SecretDvinAppsTest {
        private readonly IContainer vContainer;

        public SecretDvinAppsTest() {
            var csArgumentPrompterMock = new Mock<ICsArgumentPrompter>();
            var builder = new ContainerBuilder().RegisterForPegh(csArgumentPrompterMock.Object);
            vContainer = builder.Build();
        }

        [TestMethod]
        public async Task CanGetSecretDvinApps() {
            var repository = vContainer.Resolve<ISecretRepository>();
            var dvinAppsSecret = new SecretDvinApps();
            var errorsAndInfos = new ErrorsAndInfos();
            var dvinApps = await repository.GetAsync(dvinAppsSecret, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsTrue(dvinApps.Any(c => c.Id == "GraspNetCore"));
        }
    }
}
