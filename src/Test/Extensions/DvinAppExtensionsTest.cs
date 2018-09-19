using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public void CanGetServiceId() {
            var sut = new DvinApp { Executable = "This.Is.Not.An.exe", ReleasePort = 4711 };
            Assert.AreEqual("This.Is.Not.An.4711", sut.ServiceId());
        }

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
            fileSystemServiceMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(ComposePubXml("win7", "$(MSBuildThisFileDirectory)..\\..\\..\\..\\GraspNetCoreBin\\Publish"));
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
    }
}
