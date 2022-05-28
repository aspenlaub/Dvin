using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Components;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Repositories;

[TestClass]
public class DvinRepositoryTest {
    private readonly IContainer Container;

    public DvinRepositoryTest() {
        var builder = new ContainerBuilder().UseDvinAndPegh(new DummyCsArgumentPrompter());
        Container = builder.Build();
    }

    [TestMethod]
    public async Task CanGetDvinApps() {
        var sut = Container.Resolve<IDvinRepository>();
        var errorsAndInfos = new ErrorsAndInfos();
        var apps = await sut.LoadAsync(errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsTrue(apps.Any());
        foreach (var app in apps.Where(a => !a.Id.Contains("Grasp"))) {
            Assert.IsTrue(Directory.Exists(app.SolutionFolder), $"Folder does not exist: {app.SolutionFolder}");
            Assert.IsTrue(Directory.Exists(app.ReleaseFolder), $"Folder does not exist: {app.ReleaseFolder}");
            Assert.IsTrue(Directory.Exists(app.PublishFolder), $"Folder does not exist: {app.PublishFolder}");
            Assert.IsTrue(Directory.Exists(app.ExceptionLogFolder), $"Folder does not exist: {app.ExceptionLogFolder}");
        }
    }

    [TestMethod]
    public async Task CanGetDvinApp() {
        var sut = Container.Resolve<IDvinRepository>();
        var errorsAndInfos = new ErrorsAndInfos();
        var apps = await sut.LoadAsync(errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsTrue(apps.Any());
        var app = await sut.LoadAsync(apps[0].Id, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.AreEqual(app.Id, apps[0].Id);
    }
}