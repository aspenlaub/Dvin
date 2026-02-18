using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Components;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Seoa.Extensions;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Repositories;

[TestClass]
public class DvinRepositoryTest {
    private readonly IContainer _Container;

    public DvinRepositoryTest() {
        ContainerBuilder builder = new ContainerBuilder().UseDvinAndPegh("Dvin");
        _Container = builder.Build();
    }

    [TestMethod]
    public async Task CanGetDvinApps() {
        IDvinRepository sut = _Container.Resolve<IDvinRepository>();
        var errorsAndInfos = new ErrorsAndInfos();
        IList<DvinApp> apps = await sut.LoadAsync(errorsAndInfos);
        Assert.That.ThereWereNoErrors(errorsAndInfos);
        Assert.IsTrue(apps.Any());
        foreach (DvinApp app in apps.Where(a => !a.Id.Contains("Grasp"))) {
            Assert.IsTrue(Directory.Exists(app.SolutionFolder), $"Folder does not exist: {app.SolutionFolder}");
            Assert.IsTrue(Directory.Exists(app.ReleaseFolder), $"Folder does not exist: {app.ReleaseFolder}");
            Assert.IsTrue(Directory.Exists(app.PublishFolder), $"Folder does not exist: {app.PublishFolder}");
            Assert.IsTrue(Directory.Exists(app.ExceptionLogFolder), $"Folder does not exist: {app.ExceptionLogFolder}");
        }
    }

    [TestMethod]
    public async Task CanGetDvinApp() {
        IDvinRepository sut = _Container.Resolve<IDvinRepository>();
        var errorsAndInfos = new ErrorsAndInfos();
        IList<DvinApp> apps = await sut.LoadAsync(errorsAndInfos);
        Assert.That.ThereWereNoErrors(errorsAndInfos);
        Assert.IsTrue(apps.Any());
        DvinApp app = await sut.LoadAsync(apps[0].Id, errorsAndInfos);
        Assert.That.ThereWereNoErrors(errorsAndInfos);
        Assert.AreEqual(app.Id, apps[0].Id);
    }
}