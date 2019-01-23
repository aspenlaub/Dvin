using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Components;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions {
    public static class DvinAppExtensions {
        private const string CsProjNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

        public static void ValidatePubXml(this IDvinApp dvinApp, IErrorsAndInfos errorsAndInfos) {
            ValidatePubXml(dvinApp, new FileSystemService(), errorsAndInfos);
        }

        public static void ValidatePubXml(this IDvinApp dvinApp, IFileSystemService fileSystemService, IErrorsAndInfos errorsAndInfos) {
            if (!fileSystemService.FolderExists(dvinApp.SolutionFolder)) {
                errorsAndInfos.Errors.Add($"Folder \"{dvinApp.SolutionFolder}\" not found");
                return;
            }

            var solutionFolder = new Folder(dvinApp.SolutionFolder);
            var pubXmlFiles =  fileSystemService.ListFilesInDirectory(solutionFolder, "*.pubxml", SearchOption.AllDirectories);
            if (pubXmlFiles.Count != 1) {
                errorsAndInfos.Errors.Add($"Found {pubXmlFiles.Count} pubxml files in secret {dvinApp.Id} dvin app");
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

            var publishFolder = new Folder(dvinApp.PublishFolder);
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

        public static bool HasAppBeenPublishedAfterLatestSourceChanges(this IDvinApp dvinApp, IFileSystemService fileSystemService) {
            return HaveArtifactsBeenProducedAfterLatestSourceChanges(fileSystemService, new Folder(dvinApp.SolutionFolder), new Folder(dvinApp.PublishFolder));
        }

        public static bool HasAppBeenBuiltAfterLatestSourceChanges(this IDvinApp dvinApp, IFileSystemService fileSystemService) {
            return HaveArtifactsBeenProducedAfterLatestSourceChanges(fileSystemService, new Folder(dvinApp.SolutionFolder), new Folder(dvinApp.ReleaseFolder));
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
                .Where(f => !f.Contains(@"\%") && (f.EndsWith("dll") || f.EndsWith("config") || f.EndsWith("dll") || f.EndsWith("exe") || f.EndsWith("json")))
                .ToList();
        }

        public static void Publish(this IDvinApp dvinApp, IFileSystemService fileSystemService, IErrorsAndInfos errorsAndInfos) {
            Publish(dvinApp, fileSystemService, false, errorsAndInfos);
        }

        public static void Publish(this IDvinApp dvinApp, IFileSystemService fileSystemService, bool ignoreMissingReleaseBuild, IErrorsAndInfos errorsAndInfos) {
            if (!ignoreMissingReleaseBuild && !dvinApp.HasAppBeenBuiltAfterLatestSourceChanges(fileSystemService)) {
                errorsAndInfos.Errors.Add($"No release build for dvin app {dvinApp.Id}");
                return;
            }

            if (!fileSystemService.FolderExists(dvinApp.PublishFolder)) {
                errorsAndInfos.Errors.Add($"Folder \"{dvinApp.PublishFolder}\" not found");
                return;
            }

            if (!fileSystemService.FolderExists(dvinApp.SolutionFolder)) {
                errorsAndInfos.Errors.Add($"Folder \"{dvinApp.SolutionFolder}\" not found");
                return;
            }

            var publishedFiles = Artifacts(fileSystemService, new Folder(dvinApp.PublishFolder));
            var lastPublishedAt = publishedFiles.Any() ? publishedFiles.Max(f => fileSystemService.LastWriteTime(f)) : DateTime.Now;

            MakeCopiesOfAssemblies(new Folder(dvinApp.PublishFolder), fileSystemService);

            var projectFile = fileSystemService.ListFilesInDirectory(new Folder(dvinApp.SolutionFolder), "*.csproj", SearchOption.AllDirectories).FirstOrDefault(f => f.EndsWith(dvinApp.Id + ".csproj"));
            if (projectFile == null) {
                errorsAndInfos.Errors.Add($"No project file found for dvin app {dvinApp.Id} (must end with {dvinApp.Id}.csproj)");
                return;
            }

            var processStarter = new ProcessStarter();
            var arguments = $"publish \"{projectFile}\" -c Release --no-restore -o \"{dvinApp.PublishFolder}\"";
            using (var process = processStarter.StartProcess("dotnet", arguments, "", errorsAndInfos)) {
                if (process != null) {
                    processStarter.WaitForExit(process);
                }
            }

            if (errorsAndInfos.Infos.Any(i => i.Contains("Could not copy"))) {
                errorsAndInfos.Errors.Add($"The publish process could not copy files for dvin app {dvinApp.Id}, make sure dotnet and the app are not running");
                return;
            }

            publishedFiles = Artifacts(fileSystemService, new Folder(dvinApp.PublishFolder));
            if (!publishedFiles.Any() || lastPublishedAt >= publishedFiles.Max(f => fileSystemService.LastWriteTime(f))) {
                errorsAndInfos.Errors.Add($"Nothing was published for dvin app {dvinApp.Id}");
            }

            DeleteUnusedFileCopies(new Folder(dvinApp.PublishFolder), fileSystemService);
        }

        private static void MakeCopiesOfAssemblies(IFolder publishFolder, IFileSystemService fileSystemService) {
            DeleteUnusedFileCopies(publishFolder, fileSystemService);

            var files = fileSystemService.ListFilesInDirectory(publishFolder, "*.dll", SearchOption.AllDirectories).ToList();
            files.AddRange(fileSystemService.ListFilesInDirectory(publishFolder, "*.exe", SearchOption.AllDirectories).ToList());
            foreach (var fileName in files.Where(f => !f.Contains(@"\%"))) {
                var i = 0;
                string freshNameOne, freshNameTwo;
                do {
                    freshNameOne = RenamedFile(fileName, i);
                    freshNameTwo = RenamedFile(fileName, i + 1);
                    i += 1;
                } while (files.Contains(freshNameOne) || files.Contains(freshNameTwo));

                fileSystemService.CopyFile(fileName, freshNameTwo);
                fileSystemService.MoveFile(fileName, freshNameOne);
                fileSystemService.MoveFile(freshNameTwo, fileName);
            }

            DeleteUnusedFileCopies(publishFolder, fileSystemService);
        }

        private static void DeleteUnusedFileCopies(IFolder publishFolder, IFileSystemService fileSystemService) {
            var files = fileSystemService.ListFilesInDirectory(publishFolder, "%*.dll", SearchOption.AllDirectories).ToList();
            foreach (var file in files) {
                try {
                    fileSystemService.DeleteFile(file);
                    // ReSharper disable once EmptyGeneralCatchClause
                } catch { }
            }
        }

        private static string RenamedFile(string fileName, int counter) {
            var path = fileName.Substring(0, fileName.LastIndexOf('\\') + 1);
            return $"{path}%{counter}%{fileName.Substring(path.Length)}";
        }

        public static Process Start(this IDvinApp dvinApp, IFileSystemService fileSystemService, IErrorsAndInfos errorsAndInfos) {
            if (dvinApp.IsPortListenedTo()) {
                errorsAndInfos.Errors.Add($"Another process already listens to port {dvinApp.Port}");
                return null;
            }

            if (!fileSystemService.FolderExists(dvinApp.PublishFolder)) {
                errorsAndInfos.Errors.Add($"Folder \"{dvinApp.PublishFolder}\" not found");
                return null;
            }

            var runner = new ProcessStarter();
            var process = runner.StartProcess("dotnet", dvinApp.Executable, dvinApp.PublishFolder, errorsAndInfos);
            return process;
        }

        public static DateTime LastPublishedAt(this IDvinApp dvinApp, IFileSystemService fileSystemService) {
            var publishedFiles = Artifacts(fileSystemService, new Folder(dvinApp.PublishFolder));
            return publishedFiles.Any() ? publishedFiles.Max(f => fileSystemService.LastWriteTime(f)) : DateTime.Now;
        }
    }
}
