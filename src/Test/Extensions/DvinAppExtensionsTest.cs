using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Components;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Entities;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Extensions {
    [TestClass]
    public class DvinAppExtensionsTest {
        [TestMethod]
        public void CanValidatePubXml() {
            var fileSystemServiceMock = new Mock<IFileSystemService>();
            var machineId = Environment.MachineName;
            var notTheMachineId = machineId + machineId;
            var sut = new DvinApp();
            const string solutionFolder = @"D:\Users\Alice\GraspNetCore";
            const string publishFolder = @"D:\Users\Alice\GraspNetCoreBin\Publish";
            sut.DvinAppFolders.Add(new DvinAppFolder { MachineId = machineId, SolutionFolder = solutionFolder, PublishFolder = publishFolder });
            fileSystemServiceMock.Setup(f => f.ListFilesInDirectory(It.IsAny<IFolder>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns(new List<string>());
            var errorsAndInfos = new ErrorsAndInfos();
            sut.ValidatePubXml(notTheMachineId, fileSystemServiceMock.Object, errorsAndInfos);
            Assert.IsTrue(errorsAndInfos.Errors.Any(e => e.StartsWith($"No folders specified for {notTheMachineId}")));

            errorsAndInfos = new ErrorsAndInfos();
            sut.ValidatePubXml(machineId, fileSystemServiceMock.Object, errorsAndInfos);
            Assert.IsTrue(errorsAndInfos.Errors.Any(e => e.StartsWith("Found 0 pubxml files")));

            errorsAndInfos = new ErrorsAndInfos();
            fileSystemServiceMock.Setup(f => f.ListFilesInDirectory(It.IsAny<IFolder>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns(new List<string> { "1", "2" });
            sut.ValidatePubXml(machineId, fileSystemServiceMock.Object, errorsAndInfos);
            Assert.IsTrue(errorsAndInfos.Errors.Any(e => e.StartsWith("Found 2 pubxml files")));

            errorsAndInfos = new ErrorsAndInfos();
            fileSystemServiceMock.Setup(f => f.ListFilesInDirectory(It.IsAny<IFolder>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns(new List<string> { solutionFolder + @"\Properties\publishProfile.pubxml" });
            fileSystemServiceMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(ComposePubXml(null, null));
            sut.ValidatePubXml(machineId, fileSystemServiceMock.Object, errorsAndInfos);
            Assert.IsTrue(errorsAndInfos.Errors.Any(e => e.StartsWith("RuntimeIdentifier element not found")));

            errorsAndInfos = new ErrorsAndInfos();
            fileSystemServiceMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(ComposePubXml("abc", null));
            sut.ValidatePubXml(machineId, fileSystemServiceMock.Object, errorsAndInfos);
            Assert.IsTrue(errorsAndInfos.Errors.Any(e => e.EndsWith("does not start with win")));

            errorsAndInfos = new ErrorsAndInfos();
            fileSystemServiceMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(ComposePubXml("win7", null));
            sut.ValidatePubXml(machineId, fileSystemServiceMock.Object, errorsAndInfos);
            Assert.IsTrue(errorsAndInfos.Errors.Any(e => e.StartsWith("publishUrl element not found")));

            errorsAndInfos = new ErrorsAndInfos();
            fileSystemServiceMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(ComposePubXml("win7", "abc"));
            sut.ValidatePubXml(machineId, fileSystemServiceMock.Object, errorsAndInfos);
            Assert.IsTrue(errorsAndInfos.Errors.Any(e => e.EndsWith("does not start with $(MSBuildThisFileDirectory)")));

            errorsAndInfos = new ErrorsAndInfos();
            fileSystemServiceMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(ComposePubXml("win7", "$(MSBuildThisFileDirectory)"));
            sut.ValidatePubXml(machineId, fileSystemServiceMock.Object, errorsAndInfos);
            Assert.IsTrue(errorsAndInfos.Errors.Any(e => e.Contains("should be $(MSBuildThisFileDirectory)")));

            errorsAndInfos = new ErrorsAndInfos();
            fileSystemServiceMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(ComposePubXml("win7", "$(MSBuildThisFileDirectory)..\\..\\GraspNetCoreBin\\Publish"));
            sut.ValidatePubXml(machineId, fileSystemServiceMock.Object, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), string.Join("\r\n", errorsAndInfos.Errors));

            errorsAndInfos = new ErrorsAndInfos();
            sut.ValidatePubXml(errorsAndInfos);
            Assert.IsTrue(errorsAndInfos.Errors.Any(e => e.StartsWith("Found 0 pubxml files")));
        }

        private string ComposePubXml(string runtimeIdentifier, string publishUrl) {
            var s = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>"
                    + "<Project ToolsVersion=\"4.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><PropertyGroup>";

            if (!string.IsNullOrWhiteSpace(runtimeIdentifier)) {
                s = s + $"<RuntimeIdentifier>{runtimeIdentifier}</RuntimeIdentifier>";
            }

            if (!string.IsNullOrWhiteSpace(publishUrl)) {
                s = s + $"<publishUrl>{publishUrl}</publishUrl>";
            }
            s = s + "</PropertyGroup></Project>";
            return s;
        }

        [TestMethod]
        public void CanCheckIfPortIsListenedTo() {
            var processStarter = new ProcessStarter();
            var errorsAndInfos = new ErrorsAndInfos();
            using (var process = processStarter.StartProcess("netstat", "-n -a", "", errorsAndInfos)) {
                processStarter.WaitForExit(process);
            }

            var ports = new List<int>();
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach(var s in errorsAndInfos.Infos.Where(i => i.Contains("TCP") && i.Contains("LISTENING") && i.Contains(':'))) {
                var pos = s.IndexOf(':');
                var pos2 = s.IndexOf(' ', pos);

                if (!int.TryParse(s.Substring(pos + 1, pos2 - pos - 1), out var port)) { continue; }
                if (ports.Contains(port)) { continue; }

                ports.Add(port);
            }

            var sutMock = new Mock<IDvinApp>();
            var sut = sutMock.Object;
            foreach (var port in ports) {
                sutMock.SetupGet(d => d.Port).Returns(port);
                Assert.IsTrue(sut.IsPortListenedTo(), $"Port {port} is not listened to");
            }

            for (var port = 4711; port < 4722; port++) {
                if (ports.Contains(port)) { continue; }

                sutMock.SetupGet(d => d.Port).Returns(port);
                Assert.IsFalse(sut.IsPortListenedTo(), $"Port {port} is listened to");
            }
        }

        [TestMethod]
        public void CanCheckIfAppHasBeenPublished() {
            var fileSystemServiceMock = new Mock<IFileSystemService>();
            var machineId = Environment.MachineName;
            var sut = new DvinApp();
            const string solutionFolder = @"D:\Users\Alice\GraspNetCore";
            const string publishFolder = @"D:\Users\Alice\GraspNetCoreBin\Publish";
            sut.DvinAppFolders.Add(new DvinAppFolder { MachineId = machineId, SolutionFolder = solutionFolder, PublishFolder = publishFolder });
            fileSystemServiceMock.Setup(f => f.ListFilesInDirectory(It.IsAny<IFolder>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns<IFolder, string, SearchOption>((f, p, s) => {
                return new List<string> { f.FullName + @"\something.json" };
            });
            fileSystemServiceMock.Setup(f => f.LastWriteTime(It.IsAny<string>())).Returns<string>(f => {
                return f.StartsWith(publishFolder) ? DateTime.Now : DateTime.Now.AddMinutes(1);
            });
            Assert.IsFalse(sut.HasAppBeenPublishedAfterLatestSourceChanges(machineId, fileSystemServiceMock.Object));
            fileSystemServiceMock.Setup(f => f.LastWriteTime(It.IsAny<string>())).Returns<string>(f => {
                return f.StartsWith(publishFolder) ? DateTime.Now.AddMinutes(1) : DateTime.Now;
            });
            Assert.IsTrue(sut.HasAppBeenPublishedAfterLatestSourceChanges(machineId, fileSystemServiceMock.Object));
        }
    }
}
