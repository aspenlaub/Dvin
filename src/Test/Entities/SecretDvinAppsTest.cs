using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Entities;

[TestClass]
public class SecretDvinAppsTest {
    private readonly IContainer _Container;

    public SecretDvinAppsTest() {
        var builder = new ContainerBuilder().UsePegh("Dvin", new DummyCsArgumentPrompter());
        _Container = builder.Build();
    }

    [TestMethod]
    public async Task CanGetSecretDvinApps() {
        var repository = _Container.Resolve<ISecretRepository>();
        var dvinAppsSecret = new SecretDvinApps();
        var errorsAndInfos = new ErrorsAndInfos();
        var dvinApps = await repository.GetAsync(dvinAppsSecret, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsTrue(dvinApps.Any(c => c.Id == "GraspNetCore"));
    }
}