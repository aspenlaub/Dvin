#load "solution.cake"
#addin nuget:?package=Newtonsoft.Json
#addin nuget:?package=Cake.Git
#addin nuget:?package=Nuget.Client&loaddependencies=true
#addin nuget:?package=Nuget.Protocol.Core.v3&loaddependencies=true
#addin nuget:?package=Microsoft.Win32.Registry
#addin nuget:?package=SharpZipLib.NETStandard
#addin nuget:https://www.aspenlaub.net/nuget/?package=Aspenlaub.Net.GitHub.CSharp.Pegh
#addin nuget:https://www.aspenlaub.net/nuget/?package=Aspenlaub.Net.GitHub.CSharp.Shatilaya

using Folder = Aspenlaub.Net.GitHub.CSharp.Pegh.Entities.Folder;
using FolderUpdater = Aspenlaub.Net.GitHub.CSharp.Pegh.Components.FolderUpdater;
using FolderUpdateMethod = Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces.FolderUpdateMethod;
using ErrorsAndInfos = Aspenlaub.Net.GitHub.CSharp.Pegh.Entities.ErrorsAndInfos;
using FolderExtensions = Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions.FolderExtensions;
using Regex = System.Text.RegularExpressions.Regex;
using ComponentProvider = Aspenlaub.Net.GitHub.CSharp.Shatilaya.ComponentProvider;
using DeveloperSettingsSecret = Aspenlaub.Net.GitHub.CSharp.Shatilaya.Entities.DeveloperSettingsSecret;

masterDebugBinFolder = MakeAbsolute(Directory(masterDebugBinFolder)).FullPath;
masterReleaseBinFolder = MakeAbsolute(Directory(masterReleaseBinFolder)).FullPath;

var target = Argument("target", "Default");

var solutionId = solution.Substring(solution.LastIndexOf('/') + 1).Replace(".sln", "");
var oldArtifactsFolder = MakeAbsolute(Directory("./artifacts")).FullPath;
var debugBinFolder = MakeAbsolute(Directory("./src/bin/Debug")).FullPath;
var releaseBinFolder = MakeAbsolute(Directory("./src/bin/Release")).FullPath;
var testResultsFolder = MakeAbsolute(Directory("./TestResults")).FullPath;
var tempFolder = MakeAbsolute(Directory("./temp")).FullPath;
var repositoryFolder = MakeAbsolute(DirectoryPath.FromString(".")).FullPath;

var buildCakeFileName = MakeAbsolute(Directory(".")).FullPath + "/build.cake";
var tempCakeBuildFileName = tempFolder + "/build.cake.new";

var currentGitBranch = GitBranchCurrent(DirectoryPath.FromString("."));
var latestBuildCakeUrl = "https://raw.githubusercontent.com/aspenlaub/Shatilaya/master/build.cake?g=" + System.Guid.NewGuid();
var componentProvider = new ComponentProvider();
var toolsVersion = componentProvider.ToolsVersionFinder.LatestAvailableToolsVersion();
var toolsVersionEnum = toolsVersion >= 15 ? MSBuildToolVersion.VS2017 : MSBuildToolVersion.NET46;

var projectErrorsAndInfos = new ErrorsAndInfos();
var projectLogic = componentProvider.ProjectLogic;
var projectFactory = componentProvider.ProjectFactory;
var solutionFileFullName = (MakeAbsolute(DirectoryPath.FromString("./src")).FullPath + '\\' + solutionId + ".sln").Replace('/', '\\');

Setup(ctx => { 
  Information("Repository folder is: " + repositoryFolder);
  Information("Solution is: " + solution);
  Information("Solution ID is: " + solutionId);
  Information("Target is: " + target);
  Information("Debug bin folder is: " + debugBinFolder);
  Information("Release bin folder is: " + releaseBinFolder);
  Information("Current GIT branch is: " + currentGitBranch.FriendlyName);
  Information("Build cake is: " + buildCakeFileName);
  Information("Latest build cake URL is: " + latestBuildCakeUrl);
  Information("Tools version is: " + toolsVersion);
});

Task("UpdateBuildCake")
  .Description("Update build.cake")
  .Does(() => {
    var oldContents = System.IO.File.ReadAllText(buildCakeFileName);
    if (!System.IO.Directory.Exists(tempFolder)) {
      System.IO.Directory.CreateDirectory(tempFolder);
    }
    if (System.IO.File.Exists(tempCakeBuildFileName)) {
      System.IO.File.Delete(tempCakeBuildFileName);
    }
    using (var webClient = new System.Net.WebClient()) {
      webClient.DownloadFile(latestBuildCakeUrl, tempCakeBuildFileName);
    }
    if (Regex.Replace(oldContents, @"\s", "") != Regex.Replace(System.IO.File.ReadAllText(tempCakeBuildFileName), @"\s", "")) {
      System.IO.File.Delete(buildCakeFileName);
      System.IO.File.Move(tempCakeBuildFileName, buildCakeFileName); 
      throw new Exception("Your build.cake file has been updated. Please check it in and then retry running it.");
    } else {
      System.IO.File.Delete(tempCakeBuildFileName);
    }
  });

Task("Clean")
  .Description("Clean up artifacts and intermediate output folder")
  .Does(() => {
    CleanDirectory(oldArtifactsFolder); 
    CleanDirectory(debugBinFolder); 
    CleanDirectory(releaseBinFolder); 
  });

Task("Restore")
  .Description("Restore nuget packages")
  .Does(() => {
    var configFile = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\NuGet\nuget.config";   
    if (!System.IO.File.Exists(configFile)) {
       throw new Exception(string.Format("Nuget configuration file \"{0}\" not found", configFile));
    }
    NuGetRestore(solution, new NuGetRestoreSettings { ConfigFile = configFile });
  });

Task("Pull")
  .Description("Pull latest changes")
  .Does(async () => {
    var developerSettingsSecret = new DeveloperSettingsSecret();
    var pullErrorsAndInfos = new ErrorsAndInfos();
    var developerSettings = await componentProvider.PeghComponentProvider.SecretRepository.GetAsync(developerSettingsSecret, pullErrorsAndInfos);
    if (pullErrorsAndInfos.Errors.Any()) {
      throw new Exception(string.Join("\r\n", pullErrorsAndInfos.Errors));
    }

    GitPull(repositoryFolder, developerSettings.Author, developerSettings.Email);
  });

Task("UpdateNuspec")
  .Description("Update nuspec if necessary")
  .Does(async () => {
    var solutionFileFullName = solution.Replace('/', '\\');
    var nuSpecFile = solutionFileFullName.Replace(".sln", ".nuspec");
    var nuSpecErrorsAndInfos = new ErrorsAndInfos();
    var headTipIdSha = componentProvider.GitUtilities.HeadTipIdSha(new Folder(repositoryFolder));
    await componentProvider.NuSpecCreator.CreateNuSpecFileIfRequiredOrPresentAsync(true, solutionFileFullName, new List<string> { headTipIdSha }, nuSpecErrorsAndInfos);
    if (nuSpecErrorsAndInfos.Errors.Any()) {
      throw new Exception(string.Join("\r\n", nuSpecErrorsAndInfos.Errors));
    }
  });

Task("VerifyThatThereAreNoUncommittedChanges")
  .Description("Verify that there are no uncommitted changes")
  .Does(() => {
    var uncommittedErrorsAndInfos = new ErrorsAndInfos();
    componentProvider.GitUtilities.VerifyThatThereAreNoUncommittedChanges(new Folder(repositoryFolder), uncommittedErrorsAndInfos);
    if (uncommittedErrorsAndInfos.Errors.Any()) {
      throw new Exception(string.Join("\r\n", uncommittedErrorsAndInfos.Errors));
    }
  });

Task("VerifyThatDevelopmentBranchIsAheadOfMaster")
  .WithCriteria(() => currentGitBranch.FriendlyName != "master")
  .Description("Verify that if the development branch is at least one commit after the master")
  .Does(() => {
    if (!componentProvider.GitUtilities.IsBranchAheadOfMaster(new Folder(repositoryFolder))) {
      throw new Exception("Branch must be at least one commit ahead of the origin/master");
    }
  });

Task("VerifyThatMasterBranchDoesNotHaveOpenPullRequests")
  .WithCriteria(() => currentGitBranch.FriendlyName == "master")
  .Description("Verify that the master branch does not have open pull requests")
  .Does(async () => {
    var noPullRequestsErrorsAndInfos = new ErrorsAndInfos();
    bool thereAreOpenPullRequests;
    if (solutionSpecialSettingsDictionary.ContainsKey("PullRequestsToIgnore")) {
      thereAreOpenPullRequests = await componentProvider.GitHubUtilities.HasOpenPullRequestAsync(new Folder(repositoryFolder), solutionSpecialSettingsDictionary["PullRequestsToIgnore"], noPullRequestsErrorsAndInfos);
    } else {
      thereAreOpenPullRequests = await componentProvider.GitHubUtilities.HasOpenPullRequestAsync(new Folder(repositoryFolder), noPullRequestsErrorsAndInfos);
    }
    if (thereAreOpenPullRequests) {
      throw new Exception("There are open pull requests");
    }
    if (noPullRequestsErrorsAndInfos.Errors.Any()) {
      throw new Exception(string.Join("\r\n", noPullRequestsErrorsAndInfos.Errors));
    }
  });

Task("VerifyThatDevelopmentBranchDoesNotHaveOpenPullRequests")
  .WithCriteria(() => currentGitBranch.FriendlyName != "master")
  .Description("Verify that the master branch does not have open pull requests for the checked out development branch")
  .Does(async () => {
    var noPullRequestsErrorsAndInfos = new ErrorsAndInfos();
    bool thereAreOpenPullRequests;
    thereAreOpenPullRequests = await componentProvider.GitHubUtilities.HasOpenPullRequestForThisBranchAsync(new Folder(repositoryFolder), noPullRequestsErrorsAndInfos);
    if (thereAreOpenPullRequests) {
      throw new Exception("There are open pull requests for this development branch");
    }
    if (noPullRequestsErrorsAndInfos.Errors.Any()) {
      throw new Exception(string.Join("\r\n", noPullRequestsErrorsAndInfos.Errors));
    }
  });

Task("VerifyThatPullRequestExistsForDevelopmentBranchHeadTip")
  .WithCriteria(() => currentGitBranch.FriendlyName != "master")
  .Description("Verify that the master branch does have a pull request for the checked out development branch head tip")
  .Does(async () => {
    var noPullRequestsErrorsAndInfos = new ErrorsAndInfos();
    bool thereArePullRequests;
    thereArePullRequests = await componentProvider.GitHubUtilities.HasPullRequestForThisBranchAndItsHeadTipAsync(new Folder(repositoryFolder), noPullRequestsErrorsAndInfos);
    if (!thereArePullRequests) {
      throw new Exception("There is no pull request for this development branch and its head tip");
    }
    if (noPullRequestsErrorsAndInfos.Errors.Any()) {
      throw new Exception(string.Join("\r\n", noPullRequestsErrorsAndInfos.Errors));
    }
  });
  
Task("DebugBuild")
  .Description("Build solution in Debug")
  .Does(() => {
    MSBuild(solution, settings 
      => settings
        .SetConfiguration("Debug")
        .SetVerbosity(Verbosity.Minimal)
        .UseToolVersion(toolsVersionEnum)
        .WithProperty("Platform", "Any CPU")
    );
  });

Task("RunTestsOnDebugArtifacts")
  .Description("Run unit tests on Debug artifacts")
  .Does(() => {
      var projectFiles = GetFiles("./src/**/*Test.csproj");
      foreach(var projectFile in projectFiles) {
        var project = projectFactory.Load(solutionFileFullName, projectFile.FullPath, projectErrorsAndInfos);
        if (projectErrorsAndInfos.Errors.Any()) {
            throw new Exception(string.Join("\r\n", projectErrorsAndInfos.Errors));
        }
        Information("Running tests in " + projectFile.FullPath);
        if (projectLogic.TargetsOldFramework(project)) {
          var outputPath = project.PropertyGroups.Where(g => g.Condition.Contains("Debug")).Select(g => g.OutputPath).Where(p => p != "").First();
          outputPath = (outputPath.Contains(':') ? "" : projectFile.FullPath.Substring(0, projectFile.FullPath.LastIndexOf('/') + 1)) + outputPath.Replace('\\', '/');
          if (!outputPath.EndsWith("/")) { outputPath = outputPath + '/'; }
          Information("Output path is " + outputPath);
          var vsTestExe = componentProvider.ExecutableFinder.FindVsTestExe(toolsVersion);
          if (vsTestExe == "") {
            MSTest(outputPath + "*.Test.dll", new MSTestSettings() { NoIsolation = false });
          } else {
            VSTest(outputPath + "*.Test.dll", new VSTestSettings() { Logger = "trx", InIsolation = true });
          }
        } else {
          var logFileName = testResultsFolder + @"/TestResults-"  + project.ProjectName + ".trx";
          var dotNetCoreTestSettings = new DotNetCoreTestSettings {
            Configuration = "Debug", NoRestore = true, NoBuild = true,
            ArgumentCustomization = args => args.Append("--logger \"trx;LogFileName=" + logFileName + "\"")
          };
          DotNetCoreTest(projectFile.FullPath, dotNetCoreTestSettings);
      }
    }
    CleanDirectory(testResultsFolder); 
    DeleteDirectory(testResultsFolder, new DeleteDirectorySettings { Recursive = false, Force = false });
  });
  
Task("CopyDebugArtifacts")
  .WithCriteria(() => currentGitBranch.FriendlyName == "master")
  .Description("Copy Debug artifacts to master Debug binaries folder")
  .Does(() => {
    var updater = new FolderUpdater();
    var updaterErrorsAndInfos = new ErrorsAndInfos();
    updater.UpdateFolder(new Folder(debugBinFolder.Replace('/', '\\')), new Folder(masterDebugBinFolder.Replace('/', '\\')), 
      FolderUpdateMethod.Assemblies, updaterErrorsAndInfos);
    if (updaterErrorsAndInfos.Errors.Any()) {
      throw new Exception(string.Join("\r\n", updaterErrorsAndInfos.Errors));
    }
  });

Task("ReleaseBuild")
  .Description("Build solution in Release and clean up intermediate output folder")
  .Does(() => {
    MSBuild(solution, settings 
      => settings
        .SetConfiguration("Release")
        .SetVerbosity(Verbosity.Minimal)
        .UseToolVersion(toolsVersionEnum)
        .WithProperty("Platform", "Any CPU")
    );
  });

Task("RunTestsOnReleaseArtifacts")
  .Description("Run unit tests on Release artifacts")
  .Does(() => {
      var projectFiles = GetFiles("./src/**/*Test.csproj");
      foreach(var projectFile in projectFiles) {
        var project = projectFactory.Load(solutionFileFullName, projectFile.FullPath, projectErrorsAndInfos);
        if (projectErrorsAndInfos.Errors.Any()) {
            throw new Exception(string.Join("\r\n", projectErrorsAndInfos.Errors));
        }
        Information("Running tests in " + projectFile.FullPath);
        if (projectLogic.TargetsOldFramework(project)) {
          var outputPath = project.PropertyGroups.Where(g => g.Condition.Contains("Release")).Select(g => g.OutputPath).Where(p => p != "").First();
          outputPath = (outputPath.Contains(':') ? "" : projectFile.FullPath.Substring(0, projectFile.FullPath.LastIndexOf('/') + 1)) + outputPath.Replace('\\', '/');
          if (!outputPath.EndsWith("/")) { outputPath = outputPath + '/'; }
          Information("Output path is " + outputPath);
          var vsTestExe = componentProvider.ExecutableFinder.FindVsTestExe(toolsVersion);
          if (vsTestExe == "") {
            MSTest(outputPath + "*.Test.dll", new MSTestSettings() { NoIsolation = false });
          } else {
            VSTest(outputPath + "*.Test.dll", new VSTestSettings() { Logger = "trx", InIsolation = true });
          }
        } else {
          var logFileName = testResultsFolder + @"/TestResults-"  + project.ProjectName + ".trx";
          var dotNetCoreTestSettings = new DotNetCoreTestSettings { 
            Configuration = "Release", NoRestore = true, NoBuild = true,
            ArgumentCustomization = args => args.Append("--logger \"trx;LogFileName=" + logFileName + "\"")
          };
          DotNetCoreTest(projectFile.FullPath, dotNetCoreTestSettings);
      }
    }
    CleanDirectory(testResultsFolder); 
    DeleteDirectory(testResultsFolder, new DeleteDirectorySettings { Recursive = false, Force = false });
  });

Task("CopyReleaseArtifacts")
  .WithCriteria(() => currentGitBranch.FriendlyName == "master")
  .Description("Copy Release artifacts to master Release binaries folder")
  .Does(() => {
    var updater = new FolderUpdater();
    var updaterErrorsAndInfos = new ErrorsAndInfos();
    updater.UpdateFolder(new Folder(releaseBinFolder.Replace('/', '\\')), new Folder(masterReleaseBinFolder.Replace('/', '\\')), 
      FolderUpdateMethod.Assemblies, updaterErrorsAndInfos);
    if (updaterErrorsAndInfos.Errors.Any()) {
      throw new Exception(string.Join("\r\n", updaterErrorsAndInfos.Errors));
    }
  });

Task("CreateNuGetPackage")
  .WithCriteria(() => currentGitBranch.FriendlyName == "master")
  .Description("Create nuget package in the master Release binaries folder")
  .Does(() => {
    var projectErrorsAndInfos = new ErrorsAndInfos();
    var projectLogic = componentProvider.ProjectLogic;
    var projectFactory = componentProvider.ProjectFactory;
    var solutionFileFullName = (MakeAbsolute(DirectoryPath.FromString("./src")).FullPath + '\\' + solutionId + ".sln").Replace('/', '\\');
    var project = projectFactory.Load(solutionFileFullName, solutionFileFullName.Replace(".sln", ".csproj"), projectErrorsAndInfos);
    if (!projectLogic.DoAllNetStandardOrCoreConfigurationsHaveNuspecs(project)) {
        throw new Exception("The release configuration needs a NuspecFile entry" + "\r\n" + solutionFileFullName + "\r\n" + solutionFileFullName.Replace(".sln", ".csproj"));
    }
    if (projectErrorsAndInfos.Errors.Any()) {
        throw new Exception(string.Join("\r\n", projectErrorsAndInfos.Errors));
    }
    var folder = new Folder(masterReleaseBinFolder);
    if (!FolderExtensions.LastWrittenFileFullName(folder).EndsWith("nupkg")) {
      if (projectLogic.IsANetStandardOrCoreProject(project)) {
          var settings = new DotNetCorePackSettings {
              Configuration = "Release",
              NoBuild = true, NoRestore = true,
              IncludeSymbols = false,
              OutputDirectory = masterReleaseBinFolder,
          };

          DotNetCorePack("./src/" + solutionId + ".csproj", settings);
      } else {
          var nuGetPackSettings = new NuGetPackSettings {
            BasePath = "./src/", 
            OutputDirectory = masterReleaseBinFolder, 
            IncludeReferencedProjects = true,
            Properties = new Dictionary<string, string> { { "Configuration", "Release" } }
          };

          NuGetPack("./src/" + solutionId + ".csproj", nuGetPackSettings);
      }
    }
  });

Task("PushNuGetPackage")
  .WithCriteria(() => currentGitBranch.FriendlyName == "master")
  .Description("Push nuget package")
  .Does(async () => {
    var nugetPackageToPushFinder = componentProvider.NugetPackageToPushFinder;
    var finderErrorsAndInfos = new ErrorsAndInfos();
    var packageToPush = await nugetPackageToPushFinder.FindPackageToPushAsync(new Folder(masterReleaseBinFolder.Replace('/', '\\')), new Folder(repositoryFolder.Replace('/', '\\')), solution.Replace('/', '\\'), finderErrorsAndInfos);
    if (finderErrorsAndInfos.Errors.Any()) {
      throw new Exception(string.Join("\r\n", finderErrorsAndInfos.Errors));
    }
    if (packageToPush != null && !string.IsNullOrEmpty(packageToPush.PackageFileFullName) && !string.IsNullOrEmpty(packageToPush.FeedUrl) && !string.IsNullOrEmpty(packageToPush.ApiKey)) {
      Information("Pushing " + packageToPush.PackageFileFullName + " to " + packageToPush.FeedUrl + "..");
      NuGetPush(packageToPush.PackageFileFullName, new NuGetPushSettings { Source = packageToPush.FeedUrl });
    }
  });

Task("CleanObjectFolders")
  .Description("Clean object folders")
  .Does(() => {
    foreach(var objFolder in System.IO.Directory.GetDirectories(MakeAbsolute(DirectoryPath.FromString("./src")).FullPath, "obj", SearchOption.AllDirectories).ToList()) {
        CleanDirectory(objFolder); 
        DeleteDirectory(objFolder, new DeleteDirectorySettings { Recursive = false, Force = false });
    }
  });

Task("CleanRestorePull")
  .Description("Clean, restore packages, pull changes, update nuspec")
  .IsDependentOn("Clean").IsDependentOn("Pull").IsDependentOn("Restore").Does(() => {
  });

Task("BuildAndTestDebugAndRelease")
  .Description("Build and test debug and release configuration")
  .IsDependentOn("DebugBuild").IsDependentOn("RunTestsOnDebugArtifacts").IsDependentOn("CopyDebugArtifacts")
  .IsDependentOn("ReleaseBuild").IsDependentOn("RunTestsOnReleaseArtifacts").IsDependentOn("CopyReleaseArtifacts").Does(() => {
  });

Task("IgnoreOutdatedBuildCakePendingChangesAndDoCreateOrPushPackage")
  .Description("Default except check for outdated build.cake, except check for pending changes and except nuget create and push")
  .IsDependentOn("CleanRestorePull").IsDependentOn("BuildAndTestDebugAndRelease")
  .IsDependentOn("UpdateNuspec").Does(() => {
  });

Task("IgnoreOutdatedBuildCakePendingChangesAndDoNotPush")
  .Description("Default except check for outdated build.cake, except check for pending changes and except nuget push")
  .IsDependentOn("IgnoreOutdatedBuildCakePendingChangesAndDoCreateOrPushPackage").IsDependentOn("CreateNuGetPackage").Does(() => {
  });

Task("IgnoreOutdatedBuildCakePendingChanges")
  .Description("Default except check for outdated build.cake and except check for pending changes")
  .IsDependentOn("IgnoreOutdatedBuildCakePendingChangesAndDoNotPush").IsDependentOn("PushNuGetPackage").IsDependentOn("CleanObjectFolders").Does(() => {
  });

Task("IgnoreOutdatedBuildCakeAndDoNotPush")
  .Description("Default except check for outdated build.cake and except nuget push")
  .IsDependentOn("CleanRestorePull").IsDependentOn("VerifyThatThereAreNoUncommittedChanges").IsDependentOn("VerifyThatDevelopmentBranchIsAheadOfMaster")
  .IsDependentOn("VerifyThatMasterBranchDoesNotHaveOpenPullRequests").IsDependentOn("VerifyThatDevelopmentBranchDoesNotHaveOpenPullRequests").IsDependentOn("VerifyThatPullRequestExistsForDevelopmentBranchHeadTip")
  .IsDependentOn("BuildAndTestDebugAndRelease").IsDependentOn("UpdateNuspec").IsDependentOn("CreateNuGetPackage")
  .Does(() => {
  });

Task("LittleThings")
  .Description("Default but do not build or test in debug or release, and do not create or push nuget package")
  .IsDependentOn("CleanRestorePull").IsDependentOn("UpdateBuildCake")
  .IsDependentOn("VerifyThatThereAreNoUncommittedChanges").IsDependentOn("VerifyThatDevelopmentBranchIsAheadOfMaster")
  .IsDependentOn("VerifyThatMasterBranchDoesNotHaveOpenPullRequests").IsDependentOn("VerifyThatDevelopmentBranchDoesNotHaveOpenPullRequests").IsDependentOn("VerifyThatPullRequestExistsForDevelopmentBranchHeadTip")
  .Does(() => {
  });

Task("Default")
  .IsDependentOn("LittleThings").IsDependentOn("BuildAndTestDebugAndRelease")
  .IsDependentOn("UpdateNuspec").IsDependentOn("CreateNuGetPackage").IsDependentOn("PushNuGetPackage").IsDependentOn("CleanObjectFolders").Does(() => {
  });

RunTarget(target);