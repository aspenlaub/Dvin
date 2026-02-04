using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Components;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Helpers;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Extensions;

[TestClass]
public class DvinAppExtensionsTest {
    private readonly IContainer _Container;

    public DvinAppExtensionsTest() {
        ContainerBuilder builder = new ContainerBuilder().UseDvinAndPegh("Dvin");
        _Container = builder.Build();
    }

    [TestMethod]
    public void CanValidatePubXml() {
        var fileSystemServiceMock = new Mock<IFileSystemService>();
        const string solutionFolder = @"D:\Users\Alice\GraspNetCore";
        const string publishFolder = @"D:\Users\Alice\GraspNetCoreBin\Publish";
        var sut = new DvinApp {
            SolutionFolder = solutionFolder,
            PublishFolder = publishFolder
        };
        fileSystemServiceMock.Setup(f => f.ListFilesInDirectory(It.IsAny<IFolder>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns([]);
        fileSystemServiceMock.Setup(f => f.FolderExists(It.IsAny<string>())).Returns(true);
        var errorsAndInfos = new ErrorsAndInfos();

        sut.ValidatePubXml(fileSystemServiceMock.Object, errorsAndInfos);
        Assert.Contains(e => e.StartsWith("Found 0 pubxml files"), errorsAndInfos.Errors);

        errorsAndInfos = new ErrorsAndInfos();
        fileSystemServiceMock.Setup(f => f.ListFilesInDirectory(It.IsAny<IFolder>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns(["1", "2"]);
        sut.ValidatePubXml(fileSystemServiceMock.Object, errorsAndInfos);
        Assert.Contains(e => e.StartsWith("Found 2 pubxml files"), errorsAndInfos.Errors);

        errorsAndInfos = new ErrorsAndInfos();
        fileSystemServiceMock.Setup(f => f.ListFilesInDirectory(It.IsAny<IFolder>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns([solutionFolder + @"\Properties\publishProfile.pubxml"]);
        fileSystemServiceMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(ComposePubXml(null));
        sut.ValidatePubXml(fileSystemServiceMock.Object, errorsAndInfos);
        Assert.Contains(e => e.StartsWith("publishUrl element not found"), errorsAndInfos.Errors);

        errorsAndInfos = new ErrorsAndInfos();
        fileSystemServiceMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(ComposePubXml("abc"));
        sut.ValidatePubXml(fileSystemServiceMock.Object, errorsAndInfos);
        Assert.Contains(e => e.EndsWith("does not start with $(MSBuildThisFileDirectory)"), errorsAndInfos.Errors);

        errorsAndInfos = new ErrorsAndInfos();
        fileSystemServiceMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(ComposePubXml("$(MSBuildThisFileDirectory)"));
        sut.ValidatePubXml(fileSystemServiceMock.Object, errorsAndInfos);
        Assert.Contains(e => e.Contains("should be $(MSBuildThisFileDirectory)"), errorsAndInfos.Errors);

        errorsAndInfos = new ErrorsAndInfos();
        fileSystemServiceMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(ComposePubXml("$(MSBuildThisFileDirectory)..\\..\\GraspNetCoreBin\\Publish"));
        sut.ValidatePubXml(fileSystemServiceMock.Object, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());

        errorsAndInfos = new ErrorsAndInfos();
        sut.ValidatePubXml(errorsAndInfos);
        Assert.Contains(e => e.StartsWith("Folder") && e.EndsWith("not found"), errorsAndInfos.Errors);
    }

    private static string ComposePubXml(string publishUrl) {
        string s = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>"
                   + "<Project ToolsVersion=\"4.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><PropertyGroup>";

        if (!string.IsNullOrWhiteSpace(publishUrl)) {
            s += $"<publishUrl>{publishUrl}</publishUrl>";
        }
        s += "</PropertyGroup></Project>";
        return s;
    }

    [TestMethod]
    public void CanCheckIfPortIsListenedTo() {
        var processStarter = new ProcessStarter();
        var errorsAndInfos = new ErrorsAndInfos();
        using (Process process = processStarter.StartProcess("netstat", "-n -a", "", errorsAndInfos)) {
            if (process != null) {
                processStarter.WaitForExit(process);
            }
        }

        var ports = new List<int>();
        // ReSharper disable once LoopCanBePartlyConvertedToQuery
        foreach(string s in errorsAndInfos.Infos.Where(i => i.Contains("TCP") && (i.Contains("LISTENING") || i.Contains("ABHÖREN") || i.Contains("ABH™REN")) && i.Contains(':'))) {
            int pos = s.IndexOf(':');
            int pos2 = s.IndexOf(' ', pos);

            if (!int.TryParse(s.Substring(pos + 1, pos2 - pos - 1), out int port)) { continue; }
            if (ports.Contains(port)) { continue; }

            ports.Add(port);
        }

        var sutMock = new Mock<IDvinApp>();
        IDvinApp sut = sutMock.Object;
        foreach (int port in ports) {
            sutMock.SetupGet(d => d.Port).Returns(port);
            Assert.IsTrue(sut.IsPortListenedTo(), $"Port {port} is not listened to");
        }

        for (int port = 4711; port < 4722; port++) {
            if (ports.Contains(port)) { continue; }

            sutMock.SetupGet(d => d.Port).Returns(port);
            Assert.IsFalse(sut.IsPortListenedTo(), $"Port {port} is listened to");
        }
    }

    [TestMethod]
    public void CanCheckIfAppHasBeenPublished() {
        var fileSystemServiceMock = new Mock<IFileSystemService>();
        const string solutionFolder = @"D:\Users\Alice\GraspNetCore";
        const string releaseFolder = @"D:\Users\Alice\GraspNetCoreBin\Release";
        const string publishFolder = @"D:\Users\Alice\GraspNetCoreBin\Publish";
        var sut = new DvinApp {
            SolutionFolder = solutionFolder,
            ReleaseFolder = releaseFolder,
            PublishFolder = publishFolder
        };
        fileSystemServiceMock.Setup(f => f.ListFilesInDirectory(It.IsAny<IFolder>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns<IFolder, string, SearchOption>((f, _, _) => [f.FullName + @"\something.json"]);
        DateTime now = DateTime.Now;
        fileSystemServiceMock.Setup(f => f.LastWriteTime(It.IsAny<string>())).Returns<string>(f => f.StartsWith(publishFolder) ? now : now.AddMinutes(1));
        Assert.IsFalse(sut.HasAppBeenPublishedAfterLatestSourceChanges(fileSystemServiceMock.Object));
        fileSystemServiceMock.Setup(f => f.LastWriteTime(It.IsAny<string>())).Returns<string>(f => f.StartsWith(publishFolder) || f.StartsWith(releaseFolder) ? now.AddMinutes(1) : now);
        Assert.IsTrue(sut.HasAppBeenPublishedAfterLatestSourceChanges(fileSystemServiceMock.Object));
    }

    [TestMethod]
    public async Task CanPublishApps() {
        IDvinRepository repository = _Container.Resolve<IDvinRepository>();
        var fileSystemService = new FileSystemService();
        var errorsAndInfos = new ErrorsAndInfos();
        IList<DvinApp> apps = await repository.LoadAsync(errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        // ReSharper disable once LoopCanBePartlyConvertedToQuery
        foreach (DvinApp app in apps) {
            if (!app.HasAppBeenBuiltAfterLatestSourceChanges(fileSystemService)) { continue; }

            app.Publish(fileSystemService, errorsAndInfos);
            if (errorsAndInfos.Errors.Any(e => e.StartsWith("No folders specified"))) { continue; }

            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            break;
        }
    }

    [TestMethod]
    public async Task CanStartSampleApp() {
        IDvinRepository repository = _Container.Resolve<IDvinRepository>();
        var errorsAndInfos = new ErrorsAndInfos();
        DvinApp dvinApp = await repository.LoadAsync(Constants.DvinSampleAppId, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsNotNull(dvinApp);

        var fileSystemService = new FileSystemService();

        dvinApp.ValidatePubXml(errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());

#if DEBUG
        if (!dvinApp.HasAppBeenBuiltAfterLatestSourceChanges(fileSystemService)) {
            return;
        }
#endif

        if (!dvinApp.HasAppBeenPublishedAfterLatestSourceChanges(fileSystemService)) {
            dvinApp.Publish(fileSystemService, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        }

        Assert.IsTrue(dvinApp.HasAppBeenPublishedAfterLatestSourceChanges(fileSystemService));

        using Process process = dvinApp.Start(fileSystemService, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsNotNull(process);
        string url = $"http://localhost:{dvinApp.Port}/Home";
        Wait.Until(dvinApp.IsPortListenedTo, TimeSpan.FromSeconds(5));
        Assert.IsTrue(dvinApp.IsPortListenedTo(), errorsAndInfos.ErrorsToString());
        try {
            using var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Hello World says your dvin app", content);
        } catch {
            KillProcess(process);
            throw;
        }

        KillProcess(process);
    }

    private static void KillProcess(Process process) {
        try {
            process.Kill();
            // ReSharper disable once EmptyGeneralCatchClause
        } catch {
        }
    }

    [TestMethod]
    public async Task SampleAppCanPublishItselfWhileRunning() {
        IDvinRepository repository = _Container.Resolve<IDvinRepository>();
        var errorsAndInfos = new ErrorsAndInfos();
        DvinApp dvinApp = await repository.LoadAsync(Constants.DvinSampleAppId, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsNotNull(dvinApp);

        var fileSystemService = new FileSystemService();

        dvinApp.ValidatePubXml(errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());

        DateTime timeBeforePublishing = DateTime.Now;

        dvinApp.Publish(fileSystemService, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());

        DateTime lastPublishedAt = dvinApp.LastPublishedAt(fileSystemService);
        Assert.IsGreaterThan(timeBeforePublishing, lastPublishedAt);

        using Process process = dvinApp.Start(fileSystemService, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsNotNull(process);
        string url = $"http://localhost:{dvinApp.Port}/Publish";
        Wait.Until(dvinApp.IsPortListenedTo, TimeSpan.FromSeconds(5));
        Assert.IsTrue(dvinApp.IsPortListenedTo(), errorsAndInfos.ErrorsToString());
        try {
            using var client = new HttpClient();
            timeBeforePublishing = DateTime.Now;
            HttpResponseMessage response = await client.GetAsync(url);
            string content = await response.Content.ReadAsStringAsync();
            Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode, content);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Your dvin app just published itself", content, content);
            lastPublishedAt = dvinApp.LastPublishedAt(fileSystemService);
            Assert.IsGreaterThan(timeBeforePublishing, lastPublishedAt);
        } catch {
            KillProcess(process);
            throw;
        }

        KillProcess(process);
    }

}