using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static void ValidatePubXml(this IDvinApp dvinApp, IErrorsAndInfos errorsAndInfos) {
            ValidatePubXml(dvinApp, Environment.MachineName, new FileSystemService(), errorsAndInfos);
        }

        public static DvinAppFolder FolderOnMachine(this IDvinApp dvinApp, string machineId) {
            return dvinApp.DvinAppFolders.FirstOrDefault(d => d.MachineId.ToLowerInvariant() == machineId.ToLowerInvariant());
        }

        public static void ValidatePubXml(this IDvinApp dvinApp, string machineId, IFileSystemService fileSystemService, IErrorsAndInfos errorsAndInfos) {
            var dvinAppFolder = dvinApp.FolderOnMachine(machineId);
            if (dvinAppFolder == null) {
                errorsAndInfos.Errors.Add($"No folders specified for {machineId} in secret {dvinApp.Id} dvin app");
                return;
            }

            if (!fileSystemService.FolderExists(dvinAppFolder.SolutionFolder)) {
                errorsAndInfos.Errors.Add($"Folder \"{dvinAppFolder.SolutionFolder}\" not found");
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

            var expectedPublishUrlElement = "$(MSBuildThisFileDirectory)" + string.Join("", pubXmlFile.Substring(pos + 1).ToCharArray().Where(c => c == '\\').Select(c => @"..\")) + publishFolder.FullName.Substring(pos + 1);

            if (publishUrlElementValue == expectedPublishUrlElement) {
                return;
            }

            errorsAndInfos.Errors.Add($"publishUrl element in {pubXmlFile} should be {expectedPublishUrlElement}, but it is {publishUrlElementValue}");
        }

        public static bool IsPortListenedTo(this IDvinApp dvinApp) {
            var processStarter = new ProcessStarter();
            var errorsAndInfos = new ErrorsAndInfos();
            using (var process = processStarter.StartProcess("netstat", "-n -a", "", errorsAndInfos)) {
                if (process != null) { 
                    processStarter.WaitForExit(process);
                }
            }
            return errorsAndInfos.Infos.Any(i => i.Contains("TCP") && i.Contains("LISTENING") && i.Contains($":{dvinApp.Port} "));
        }

        public static bool HasAppBeenPublishedAfterLatestSourceChanges(this IDvinApp dvinApp, string machineId, IFileSystemService fileSystemService) {
            var dvinAppFolder = dvinApp.FolderOnMachine(machineId);
            return dvinAppFolder != null && HaveArtifactsBeenProducedAfterLatestSourceChanges(fileSystemService, new Folder(dvinAppFolder.SolutionFolder), new Folder(dvinAppFolder.PublishFolder));
        }

        public static bool HasAppBeenBuiltAfterLatestSourceChanges(this IDvinApp dvinApp, string machineId,
            IFileSystemService fileSystemService) {
            var dvinAppFolder = dvinApp.FolderOnMachine(machineId);
            return dvinAppFolder != null && HaveArtifactsBeenProducedAfterLatestSourceChanges(fileSystemService, new Folder(dvinAppFolder.SolutionFolder), new Folder(dvinAppFolder.ReleaseFolder));
        }

        private static bool HaveArtifactsBeenProducedAfterLatestSourceChanges(IFileSystemService fileSystemService, IFolder sourceFolder, IFolder artifactsFolder) {
            var sourceFiles = fileSystemService.ListFilesInDirectory(sourceFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.Contains(@"\bin\") && !f.Contains(@"\obj\")
                      && (f.EndsWith("cs") || f.EndsWith("csproj") || f.EndsWith("cshtml") || f.EndsWith("json"))
                ).ToList();
            if (!sourceFiles.Any()) { return false; }

            var publishedFiles = Artifacts(fileSystemService, artifactsFolder);
            if (!publishedFiles.Any()) { return false; }

            var sourceChangedAt = sourceFiles.Max(f => fileSystemService.LastWriteTime(f));
            var publishedAt = publishedFiles.Max(f => fileSystemService.LastWriteTime(f));
            return publishedAt > sourceChangedAt.AddSeconds(1);
        }

        private static List<string> Artifacts(IFileSystemService fileSystemService, IFolder artifactsFolder) {
            return fileSystemService.ListFilesInDirectory(artifactsFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith("dll") || f.EndsWith("config") || f.EndsWith("dll") || f.EndsWith("exe") || f.EndsWith("json"))
                .ToList();
        }

        public static void Publish(this IDvinApp dvinApp, IFileSystemService fileSystemService, IErrorsAndInfos errorsAndInfos) {
            var machineId = Environment.MachineName;
            var dvinAppFolder = dvinApp.FolderOnMachine(machineId);
            if (dvinAppFolder == null) {
                errorsAndInfos.Errors.Add($"No folders specified for {machineId} in secret {dvinApp.Id} dvin app");
                return;
            }

            if (!dvinApp.HasAppBeenBuiltAfterLatestSourceChanges(machineId, fileSystemService)) {
                errorsAndInfos.Errors.Add($"No release build for dvin app {dvinApp.Id} on {machineId}");
                return;
            }

            if (!fileSystemService.FolderExists(dvinAppFolder.PublishFolder)) {
                errorsAndInfos.Errors.Add($"Folder \"{dvinAppFolder.PublishFolder}\" not found");
                return;
            }

            if (!fileSystemService.FolderExists(dvinAppFolder.SolutionFolder)) {
                errorsAndInfos.Errors.Add($"Folder \"{dvinAppFolder.SolutionFolder}\" not found");
                return;
            }

            var publishedFiles = Artifacts(fileSystemService, new Folder(dvinAppFolder.PublishFolder));
            var lastPublishedAt = publishedFiles.Any() ? publishedFiles.Max(f => fileSystemService.LastWriteTime(f)) : DateTime.Now;

            var projectFile = fileSystemService.ListFilesInDirectory(new Folder(dvinAppFolder.SolutionFolder), "*.csproj", SearchOption.AllDirectories).FirstOrDefault(f => f.EndsWith(dvinApp.Id + ".csproj"));
            if (projectFile == null) {
                errorsAndInfos.Errors.Add($"No project file found for {machineId} and dvin app {dvinApp.Id} (must end with {dvinApp.Id}.csproj)");
                return;
            }

            var processStarter = new ProcessStarter();
            var arguments = $"publish \"{projectFile}\" -c Release --no-build --no-restore -o \"{dvinAppFolder.PublishFolder}\"";
            using (var process = processStarter.StartProcess("dotnet", arguments, "", errorsAndInfos)) {
                if (process != null) { 
                    processStarter.WaitForExit(process);
                }
            }

            if (errorsAndInfos.Infos.Any(i => i.Contains("Could not copy"))) {
                errorsAndInfos.Errors.Add($"The publish proceess could not copy files for {machineId} and dvin app {dvinApp.Id}, make sure dotnet and the little things are not running");
                return;
            }

            publishedFiles = Artifacts(fileSystemService, new Folder(dvinAppFolder.PublishFolder));
            if (!publishedFiles.Any() || lastPublishedAt >= publishedFiles.Max(f => fileSystemService.LastWriteTime(f))) {
                errorsAndInfos.Errors.Add($"Nothing was published for {machineId} and dvin app {dvinApp.Id}");
            }
        }

        public static Process Start(this IDvinApp dvinApp, IFileSystemService fileSystemService, IErrorsAndInfos errorsAndInfos) {
            if (dvinApp.IsPortListenedTo()) {
                errorsAndInfos.Errors.Add($"Another process already listens to port {dvinApp.Port}");
                return null;
            }

            var machineId = Environment.MachineName;
            var dvinAppFolder = dvinApp.FolderOnMachine(machineId);
            if (dvinAppFolder == null) {
                errorsAndInfos.Errors.Add($"No folders specified for {machineId} in secret {dvinApp.Id} dvin app");
                return null;
            }

            if (!fileSystemService.FolderExists(dvinAppFolder.PublishFolder)) {
                errorsAndInfos.Errors.Add($"Folder \"{dvinAppFolder.PublishFolder}\" not found");
                return null;
            }

            var runner = new ProcessStarter();
            var process = runner.StartProcess("dotnet", dvinApp.Executable, dvinAppFolder.PublishFolder, errorsAndInfos);
            return process;
        }
    }
}
