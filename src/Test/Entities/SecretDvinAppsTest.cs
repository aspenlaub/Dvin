using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Seoa.Extensions;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Entities;

[TestClass]
public class SecretDvinAppsTest {
    private readonly IContainer _Container;

    public SecretDvinAppsTest() {
        ContainerBuilder builder = new ContainerBuilder().UsePegh("Dvin");
        _Container = builder.Build();
    }

    [TestMethod]
    public async Task CanGetSecretDvinApps() {
        ISecretRepository repository = _Container.Resolve<ISecretRepository>();
        var dvinAppsSecret = new SecretDvinApps();
        var errorsAndInfos = new ErrorsAndInfos();
        DvinApps dvinApps = await repository.GetAsync(dvinAppsSecret, errorsAndInfos);
        Assert.That.ThereWereNoErrors(errorsAndInfos);
        Assert.Contains(c => c.Id == "GraspNetCore", dvinApps);
    }
}