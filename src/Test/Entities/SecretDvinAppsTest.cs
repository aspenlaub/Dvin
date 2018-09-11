using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Components;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Entities {
    [TestClass]
    public class SecretDvinAppsTest {
        [TestMethod]
        public async Task CanGetSecretDvinApps() {
            var repository = new SecretRepository(new ComponentProvider());
            var dvinAppsSecret = new SecretDvinApps();
            var errorsAndInfos = new ErrorsAndInfos();
            var dvinApps = await repository.GetAsync(dvinAppsSecret, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), string.Join("\r\n", errorsAndInfos.Errors));
            Assert.IsTrue(dvinApps.Any(c => c.Id == "GraspNetCore"));
        }
    }
}
