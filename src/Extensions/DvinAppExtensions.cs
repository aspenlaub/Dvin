using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Components;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Entities;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions {
    public static class DvinAppExtensions {
        private const string CsProjNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

        public static string ServiceId(this DvinApp dvinApp) {
            return dvinApp.Executable.Replace(".exe", $".{dvinApp.ReleasePort}");
        }

        public static void ValidatePubXml(this DvinApp dvinApp, IErrorsAndInfos errorsAndInfos) {
            ValidatePubXml(dvinApp, Environment.MachineName, new FileSystemService(), errorsAndInfos);
        }

        public static void ValidatePubXml(this DvinApp dvinApp, string machineId, IFileSystemService fileSystemService, IErrorsAndInfos errorsAndInfos) {
            var dvinAppFolder = dvinApp.DvinAppFolders.FirstOrDefault(d => d.MachineId.ToLowerInvariant() == machineId.ToLowerInvariant());
            if (dvinAppFolder == null) {
                errorsAndInfos.Errors.Add($"No folders specified for {machineId} in secret {dvinApp.Id} dvin app");
                return;
            }

            var solutionFolder = new Folder(dvinAppFolder.SolutionFolder);
            var pubXmlFiles =  fileSystemService.ListFilesInDirectory(solutionFolder, "*.pubxml", SearchOption.AllDirectories);
            if (pubXmlFiles.Count != 1) {
                errorsAndInfos.Errors.Add($"Found {pubXmlFiles.Count} pubxml files for machine {machineId} in secret {dvinApp.Id} dvin app");
                return;
            }

            XDocument document;
            var pubXmlFile = pubXmlFiles[0];
            try {
                document = XDocument.Parse(fileSystemService.ReadAllText(pubXmlFile));
            } catch {
                errorsAndInfos.Errors.Add($"Could not parse {pubXmlFile}");
                return;
            }

            var namespaceManager = new XmlNamespaceManager(new NameTable());
            namespaceManager.AddNamespace("cp", CsProjNamespace);
            var runtimeIdentifierElementValue = document.XPathSelectElement("./cp:Project/cp:PropertyGroup/cp:RuntimeIdentifier", namespaceManager)?.Value;
            if (string.IsNullOrWhiteSpace(runtimeIdentifierElementValue)) {
                errorsAndInfos.Errors.Add($"RuntimeIdentifier element not found in {pubXmlFile}");
                return;
            }

            if (!runtimeIdentifierElementValue.StartsWith(@"win")) {
                errorsAndInfos.Errors.Add($"RuntimeIdentifier element in {pubXmlFile} does not start with win");
                return;
            }

            var publishUrlElementValue = document.XPathSelectElement("./cp:Project/cp:PropertyGroup/cp:publishUrl", namespaceManager)?.Value;
            if (string.IsNullOrWhiteSpace(publishUrlElementValue)) {
                errorsAndInfos.Errors.Add($"publishUrl element not found in {pubXmlFile}");
                return;
            }

            if (!publishUrlElementValue.StartsWith(@"$(MSBuildThisFileDirectory)")) {
                errorsAndInfos.Errors.Add($"publishUrl element in {pubXmlFile} does not start with $(MSBuildThisFileDirectory)");
                return;
            }

            var publishFolder = new Folder(dvinAppFolder.PublishFolder);
            int pos;
            for (pos = 0; pos < pubXmlFile.Length && pos < publishFolder.FullName.Length && pubXmlFile[pos] == publishFolder.FullName[pos]; pos++) { }
            for (pos --; pos > 0 && pubXmlFile[pos] != '\\'; pos--) { }

            if (pos <= 0) {
                errorsAndInfos.Errors.Add($"{pubXmlFile} and {publishFolder.FullName} do not share a common prefix");
                return;
            }

            var expectedPublishUrlElement = "$(MSBuildThisFileDirectory)" + string.Join("", pubXmlFile.Substring(pos).ToCharArray().Where(c => c == '\\').Select(c => @"..\")) + publishFolder.FullName.Substring(pos + 1);

            if (publishUrlElementValue == expectedPublishUrlElement) {
                return;
            }

            errorsAndInfos.Errors.Add($"publishUrl element in {pubXmlFile} should be {expectedPublishUrlElement}, but it is {publishUrlElementValue}");
        }
    }
}
