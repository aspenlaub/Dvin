#load "solution.cake"
#addin nuget:?package=Cake.Git
#addin nuget:?package=System.Runtime.Loader
#addin nuget:?package=Microsoft.Bcl.AsyncInterfaces
#addin nuget:?package=Fusion&loaddependencies=true&version=2.0.642.960

using Regex = System.Text.RegularExpressions.Regex;
using Microsoft.Extensions.DependencyInjection;
using Autofac;
using System.Runtime.Loader;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Entities;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Components;
using Aspenlaub.Net.GitHub.CSharp.Protch;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;
using Aspenlaub.Net.GitHub.CSharp.Fusion;
using Aspenlaub.Net.GitHub.CSharp.Fusion.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Fusion.Components;
using FolderUpdateMethod = Aspenlaub.Net.GitHub.CSharp.Fusion.Interfaces.FolderUpdateMethod;

masterDebugBinFolder = MakeAbsolute(Directory(masterDebugBinFolder)).FullPath;
masterReleaseBinFolder = MakeAbsolute(Directory(masterReleaseBinFolder)).FullPath;

var target = Argument("target", "Default");

var solutionId = solution.Substring(solution.LastIndexOf('/') + 1).Replace(".sln", "");
var debugBinFolder = MakeAbsolute(Directory("./src/bin/Debug")).FullPath;
var releaseBinFolder = MakeAbsolute(Directory("./src/bin/Release")).FullPath;
var testResultsFolder = MakeAbsolute(Directory("./TestResults")).FullPath;
var tempFolder = MakeAbsolute(Directory("./temp")).FullPath;
var repositoryFolder = MakeAbsolute(DirectoryPath.FromString(".")).FullPath;

var buildCakeFileName = MakeAbsolute(Directory(".")).FullPath + "/build.cake";
var tempCakeBuildFileName = tempFolder + "/build.cake.new";

var mainNugetFeedId = NugetFeed.AspenlaubLocalFeed;

var container = FusionContainerBuilder.CreateContainerUsingFusionNuclideProtchAndGitty();
var currentGitBranch = container.Resolve<IGitUtilities>().CheckedOutBranch(new Folder(repositoryFolder));
var latestBuildCakeUrl = "https://raw.githubusercontent.com/aspenlaub/Shatilaya/master/build.cake?g=" + System.Guid.NewGuid();

var projectErrorsAndInfos = new ErrorsAndInfos();
var projectLogic = container.Resolve<IProjectLogic>();
var projectFactory = container.Resolve<IProjectFactory>();
var solutionFileFullName = (MakeAbsolute(DirectoryPath.FromString("./src")).FullPath + '\\' + solutionId + ".sln").Replace('/', '\\');

var masterReleaseBinParentFolder = new Folder(masterReleaseBinFolder.Replace('/', '\\')).ParentFolder();
var releaseBinHeadTipIdShaFile = masterReleaseBinParentFolder.FullName + '\\' + "Release.HeadTipSha.txt";

var createAndPushPackages = true;
if (solutionSpecialSettingsDictionary.ContainsKey("CreateAndPushPackages")) {
  var createAndPushPackagesText = solutionSpecialSettingsDictionary["CreateAndPushPackages"].ToUpper();
  if (createAndPushPackagesText != "TRUE" && createAndPushPackagesText != "FALSE") {
    throw new Exception("Setting CreateAndPushPackages must be true or false");
  }
  createAndPushPackages = createAndPushPackagesText == "TRUE";
}

Setup(ctx => { 
  Information("Repository folder is: " + repositoryFolder);
  Information("Solution is: " + solution);
  Information("Solution ID is: " + solutionId);
  Information("Target is: " + target);
  Information("Debug bin folder is: " + debugBinFolder);
  Information("Release bin folder is: " + releaseBinFolder);
  Information("Current GIT branch is: " + currentGitBranch);
  Information("Build cake is: " + buildCakeFileName);
  Information("Latest build cake URL is: " + latestBuildCakeUrl);
});

Task("UpdateBuildCake")
  .Description("Update cake")
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
      Information("Updating cake");
      System.IO.File.Delete(buildCakeFileName);
      System.IO.File.Move(tempCakeBuildFileName, buildCakeFileName); 
      var autoErrorsAndInfos = new ErrorsAndInfos();
      container.Resolve<IAutoCommitterAndPusher>().AutoCommitAndPushSingleCakeFileIfNecessaryAsync(mainNugetFeedId, new Folder(repositoryFolder), autoErrorsAndInfos).Wait();
      if (autoErrorsAndInfos.Errors.Any()) {
        throw new Exception(autoErrorsAndInfos.ErrorsToString());
      }
      throw new Exception("Your cake file has been updated. Please retry running it.");
    } else {
      Information("The cake is up-to-date");
      System.IO.File.Delete(tempCakeBuildFileName);
      var autoErrorsAndInfos = new ErrorsAndInfos();
      container.Resolve<IAutoCommitterAndPusher>().AutoCommitAndPushSingleCakeFileIfNecessaryAsync(mainNugetFeedId, new Folder(repositoryFolder), autoErrorsAndInfos).Wait();
      if (autoErrorsAndInfos.Errors.Any()) {
        throw new Exception(autoErrorsAndInfos.ErrorsToString());
      }
    }
    var pinErrorsAndInfos = new ErrorsAndInfos();
    container.Resolve<IPinnedAddInVersionChecker>().CheckPinnedAddInVersionsAsync(new Folder(repositoryFolder), pinErrorsAndInfos).Wait();
    if (pinErrorsAndInfos.Errors.Any()) {
      throw new Exception(pinErrorsAndInfos.ErrorsToString());
    }
  });

Task("Clean")
  .Description("Clean up artifacts and intermediate output folder")
  .Does(() => {
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
    var developerSettings = await container.Resolve<ISecretRepository>().GetAsync(developerSettingsSecret, pullErrorsAndInfos);
    if (pullErrorsAndInfos.Errors.Any()) {
      throw new Exception(pullErrorsAndInfos.ErrorsToString());
    }

    container.Resolve<IGitUtilities>().Pull(new Folder(repositoryFolder), developerSettings.Author, developerSettings.Email);
  });

Task("UpdateNuspec")
  .Description("Update nuspec if necessary")
  .Does(async () => {
    var solutionFileFullName = solution.Replace('/', '\\');
    var nuSpecFile = solutionFileFullName.Replace(".sln", ".nuspec");
    var nuSpecErrorsAndInfos = new ErrorsAndInfos();
    var headTipIdSha = container.Resolve<IGitUtilities>().HeadTipIdSha(new Folder(repositoryFolder));
    await container.Resolve<INuSpecCreator>().CreateNuSpecFileIfRequiredOrPresentAsync(true, solutionFileFullName, new List<string> { headTipIdSha }, nuSpecErrorsAndInfos);
    if (nuSpecErrorsAndInfos.Errors.Any()) {
      throw new Exception(nuSpecErrorsAndInfos.ErrorsToString());
    }
  });

Task("VerifyThatThereAreNoUncommittedChanges")
  .Description("Verify that there are no uncommitted changes")
  .Does(() => {
    var uncommittedErrorsAndInfos = new ErrorsAndInfos();
    container.Resolve<IGitUtilities>().VerifyThatThereAreNoUncommittedChanges(new Folder(repositoryFolder), uncommittedErrorsAndInfos);
    if (uncommittedErrorsAndInfos.Errors.Any()) {
      throw new Exception(uncommittedErrorsAndInfos.ErrorsToString());
    }
  });

Task("VerifyThatThereAreUncommittedChanges")
  .Description("Verify that there are uncommitted changes")
  .Does(() => {
    var uncommittedErrorsAndInfos = new ErrorsAndInfos();
    container.Resolve<IGitUtilities>().VerifyThatThereAreNoUncommittedChanges(new Folder(repositoryFolder), uncommittedErrorsAndInfos);
    if (!uncommittedErrorsAndInfos.Errors.Any()) {
      throw new Exception("The check for uncommitted changes did not fail, this is unexpected");
    }
  });

Task("VerifyThatDevelopmentBranchIsAheadOfMaster")
  .WithCriteria(() => currentGitBranch != "master")
  .Description("Verify that if the development branch is at least one commit after the master")
  .Does(() => {
    if (!container.Resolve<IGitUtilities>().IsBranchAheadOfMaster(new Folder(repositoryFolder))) {
      throw new Exception("Branch must be at least one commit ahead of the origin/master");
    }
  });

Task("VerifyThatMasterBranchDoesNotHaveOpenPullRequests")
  .WithCriteria(() => currentGitBranch == "master")
  .Description("Verify that the master branch does not have open pull requests")
  .Does(async () => {
    var noPullRequestsErrorsAndInfos = new ErrorsAndInfos();
    bool thereAreOpenPullRequests;
    if (solutionSpecialSettingsDictionary.ContainsKey("PullRequestsToIgnore")) {
      thereAreOpenPullRequests = await container.Resolve<IGitHubUtilities>().HasOpenPullRequestAsync(new Folder(repositoryFolder), solutionSpecialSettingsDictionary["PullRequestsToIgnore"], noPullRequestsErrorsAndInfos);
    } else {
      thereAreOpenPullRequests = await container.Resolve<IGitHubUtilities>().HasOpenPullRequestAsync(new Folder(repositoryFolder), noPullRequestsErrorsAndInfos);
    }
    if (thereAreOpenPullRequests) {
      throw new Exception("There are open pull requests");
    }
    if (noPullRequestsErrorsAndInfos.Errors.Any()) {
      throw new Exception(noPullRequestsErrorsAndInfos.ErrorsToString());
    }
  });

Task("VerifyThatDevelopmentBranchDoesNotHaveOpenPullRequests")
  .WithCriteria(() => currentGitBranch != "master")
  .Description("Verify that the master branch does not have open pull requests for the checked out development branch")
  .Does(async () => {
    var noPullRequestsErrorsAndInfos = new ErrorsAndInfos();
    bool thereAreOpenPullRequests;
    thereAreOpenPullRequests = await container.Resolve<IGitHubUtilities>().HasOpenPullRequestForThisBranchAsync(new Folder(repositoryFolder), noPullRequestsErrorsAndInfos);
    if (thereAreOpenPullRequests) {
      throw new Exception("There are open pull requests for this development branch");
    }
    if (noPullRequestsErrorsAndInfos.Errors.Any()) {
      throw new Exception(noPullRequestsErrorsAndInfos.ErrorsToString());
    }
  });

Task("VerifyThatPullRequestExistsForDevelopmentBranchHeadTip")
  .WithCriteria(() => currentGitBranch != "master")
  .Description("Verify that the master branch does have a pull request for the checked out development branch head tip")
  .Does(async () => {
    var noPullRequestsErrorsAndInfos = new ErrorsAndInfos();
    bool thereArePullRequests;
    thereArePullRequests = await container.Resolve<IGitHubUtilities>().HasPullRequestForThisBranchAndItsHeadTipAsync(new Folder(repositoryFolder), noPullRequestsErrorsAndInfos);
    if (!thereArePullRequests) {
      throw new Exception("There is no pull request for this development branch and its head tip");
    }
    if (noPullRequestsErrorsAndInfos.Errors.Any()) {
      throw new Exception(noPullRequestsErrorsAndInfos.ErrorsToString());
    }
  });
  
Task("DebugBuild")
  .Description("Build solution in Debug")
  .Does(() => {
    MSBuild(solution, settings 
      => settings
        .SetConfiguration("Debug")
        .SetVerbosity(Verbosity.Minimal)
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
            throw new Exception(projectErrorsAndInfos.ErrorsToString());
        }
        if (projectLogic.TargetsOldFramework(project)) {
            throw new Exception(".Net frameworks 4.6 and 4.5 are no longer supported");
        }
        Information("Running tests in " + projectFile.FullPath);
        var logFileName = testResultsFolder + @"/TestResults-"  + project.ProjectName + ".trx";
        var dotNetCoreTestSettings = new DotNetCoreTestSettings {
          Configuration = "Debug", NoRestore = true, NoBuild = true,
          ArgumentCustomization = args => args.Append("--logger \"trx;LogFileName=" + logFileName + "\"")
        };
        DotNetCoreTest(projectFile.FullPath, dotNetCoreTestSettings);
    }
    CleanDirectory(testResultsFolder); 
    DeleteDirectory(testResultsFolder, new DeleteDirectorySettings { Recursive = false, Force = false });
  });
  
Task("CopyDebugArtifacts")
  .WithCriteria(() => currentGitBranch == "master")
  .Description("Copy Debug artifacts to master Debug binaries folder")
  .Does(async () => {
    var updater = container.Resolve<IFolderUpdater>();
    var updaterErrorsAndInfos = new ErrorsAndInfos();
    var headTipIdSha = container.Resolve<IGitUtilities>().HeadTipIdSha(new Folder(repositoryFolder));
    if (!System.IO.File.Exists(releaseBinHeadTipIdShaFile)) {
      updater.UpdateFolder(new Folder(debugBinFolder.Replace('/', '\\')), new Folder(masterDebugBinFolder.Replace('/', '\\')), 
        FolderUpdateMethod.AssembliesButNotIfOnlySlightlyChanged, "Aspenlaub.Net.GitHub.CSharp." + solutionId, updaterErrorsAndInfos);
    } else {
      await updater.UpdateFolderAsync(solutionId, headTipIdSha, new Folder(debugBinFolder.Replace('/', '\\')),
        System.IO.File.ReadAllText(releaseBinHeadTipIdShaFile), new Folder(masterDebugBinFolder.Replace('/', '\\')),
        false, createAndPushPackages, mainNugetFeedId, updaterErrorsAndInfos);
    }
    updaterErrorsAndInfos.Infos.ToList().ForEach(i => Information(i));
    if (updaterErrorsAndInfos.Errors.Any()) {
      throw new Exception(updaterErrorsAndInfos.ErrorsToString());
    }
  });

Task("ReleaseBuild")
  .Description("Build solution in Release and clean up intermediate output folder")
  .Does(() => {
    MSBuild(solution, settings 
      => settings
        .SetConfiguration("Release")
        .SetVerbosity(Verbosity.Minimal)
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
            throw new Exception(projectErrorsAndInfos.ErrorsToString());
        }
        if (projectLogic.TargetsOldFramework(project)) {
            throw new Exception(".Net frameworks 4.6 and 4.5 are no longer supported");
        }
        Information("Running tests in " + projectFile.FullPath);
        var logFileName = testResultsFolder + @"/TestResults-"  + project.ProjectName + ".trx";
        var dotNetCoreTestSettings = new DotNetCoreTestSettings { 
          Configuration = "Release", NoRestore = true, NoBuild = true,
          ArgumentCustomization = args => args.Append("--logger \"trx;LogFileName=" + logFileName + "\"")
        };
        DotNetCoreTest(projectFile.FullPath, dotNetCoreTestSettings);
    }
    CleanDirectory(testResultsFolder); 
    DeleteDirectory(testResultsFolder, new DeleteDirectorySettings { Recursive = false, Force = false });
  });

Task("CopyReleaseArtifacts")
  .WithCriteria(() => currentGitBranch == "master")
  .Description("Copy Release artifacts to master Release binaries folder")
  .Does(async () => {
    var updater = container.Resolve<IFolderUpdater>();
    var updaterErrorsAndInfos = new ErrorsAndInfos();
    var headTipIdSha = container.Resolve<IGitUtilities>().HeadTipIdSha(new Folder(repositoryFolder));
    if (!System.IO.File.Exists(releaseBinHeadTipIdShaFile)) {
      updater.UpdateFolder(new Folder(releaseBinFolder.Replace('/', '\\')), new Folder(masterReleaseBinFolder.Replace('/', '\\')), 
        FolderUpdateMethod.AssembliesButNotIfOnlySlightlyChanged, "Aspenlaub.Net.GitHub.CSharp." + solutionId, updaterErrorsAndInfos);
    } else {
      await updater.UpdateFolderAsync(solutionId, headTipIdSha, new Folder(releaseBinFolder.Replace('/', '\\')),
        System.IO.File.ReadAllText(releaseBinHeadTipIdShaFile), new Folder(masterReleaseBinFolder.Replace('/', '\\')),
        true, createAndPushPackages, mainNugetFeedId, updaterErrorsAndInfos);
    }
    updaterErrorsAndInfos.Infos.ToList().ForEach(i => Information(i));
    if (updaterErrorsAndInfos.Errors.Any()) {
      throw new Exception(updaterErrorsAndInfos.ErrorsToString());
    }
    System.IO.File.WriteAllText(releaseBinHeadTipIdShaFile, headTipIdSha);
  });

Task("CreateNuGetPackage")
  .WithCriteria(() => currentGitBranch == "master" && createAndPushPackages)
  .Description("Create nuget package in the master Release binaries folder")
  .Does(() => {
    var projectErrorsAndInfos = new ErrorsAndInfos();
    var solutionFileFullName = (MakeAbsolute(DirectoryPath.FromString("./src")).FullPath + '\\' + solutionId + ".sln").Replace('/', '\\');
    var project = projectFactory.Load(solutionFileFullName, solutionFileFullName.Replace(".sln", ".csproj"), projectErrorsAndInfos);
    if (!projectLogic.DoAllNetStandardOrCoreConfigurationsHaveNuspecs(project)) {
        throw new Exception("The release configuration needs a NuspecFile entry" + "\r\n" + solutionFileFullName + "\r\n" + solutionFileFullName.Replace(".sln", ".csproj"));
    }
    if (projectErrorsAndInfos.Errors.Any()) {
        throw new Exception(projectErrorsAndInfos.ErrorsToString());
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
  .WithCriteria(() => currentGitBranch == "master" && createAndPushPackages)
  .Description("Push nuget package")
  .Does(async () => {
    var nugetPackageToPushFinder = container.Resolve<INugetPackageToPushFinder>();
    var finderErrorsAndInfos = new ErrorsAndInfos();
    var packageToPush = await nugetPackageToPushFinder.FindPackageToPushAsync(mainNugetFeedId, new Folder(masterReleaseBinFolder.Replace('/', '\\')), new Folder(repositoryFolder.Replace('/', '\\')), solution.Replace('/', '\\'), finderErrorsAndInfos);
    if (finderErrorsAndInfos.Errors.Any()) {
      throw new Exception(finderErrorsAndInfos.ErrorsToString());
    }
    var headTipSha = container.Resolve<IGitUtilities>().HeadTipIdSha(new Folder(repositoryFolder));
    if (packageToPush != null && !string.IsNullOrEmpty(packageToPush.PackageFileFullName) && !string.IsNullOrEmpty(packageToPush.FeedUrl)) {
      finderErrorsAndInfos.Infos.ToList().ForEach(i => Information(i));
      Information("Pushing " + packageToPush.PackageFileFullName + " to " + packageToPush.FeedUrl + "..");
      NuGetPush(packageToPush.PackageFileFullName, new NuGetPushSettings { Source = packageToPush.FeedUrl });
    } else {
      Information("Did not find any package to push, adding " + headTipSha + " to pushed headTipShas for " + mainNugetFeedId);
    }
    var pushedHeadTipShaRepository = container.Resolve<IPushedHeadTipShaRepository>();
    var pushedErrorsAndInfos = new ErrorsAndInfos();
    if (packageToPush != null && !string.IsNullOrEmpty(packageToPush.Id) && !string.IsNullOrEmpty(packageToPush.Version)) {
      await pushedHeadTipShaRepository.AddAsync(mainNugetFeedId, headTipSha, packageToPush.Id, packageToPush.Version, pushedErrorsAndInfos);
    } else {
      await pushedHeadTipShaRepository.AddAsync(mainNugetFeedId, headTipSha, pushedErrorsAndInfos);
    }
    if (pushedErrorsAndInfos.Errors.Any()) {
      throw new Exception(pushedErrorsAndInfos.ErrorsToString());
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
  .Description("Default except check for outdated cake, except check for pending changes and except nuget create and push")
  .IsDependentOn("CleanRestorePull").IsDependentOn("BuildAndTestDebugAndRelease")
  .IsDependentOn("UpdateNuspec").Does(() => {
  });

Task("IgnoreOutdatedBuildCakePendingChangesAndDoNotPush")
  .Description("Default except check for outdated cake, except check for pending changes and except nuget push")
  .IsDependentOn("IgnoreOutdatedBuildCakePendingChangesAndDoCreateOrPushPackage").IsDependentOn("CreateNuGetPackage").Does(() => {
  });

Task("IgnoreOutdatedBuildCakePendingChanges")
  .Description("Default except check for outdated cake and except check for pending changes")
  .IsDependentOn("IgnoreOutdatedBuildCakePendingChangesAndDoNotPush").IsDependentOn("PushNuGetPackage").IsDependentOn("CleanObjectFolders").Does(() => {
  });

Task("IgnoreOutdatedBuildCakeAndDoNotPush")
  .Description("Default except check for outdated cake and except nuget push")
  .IsDependentOn("CleanRestorePull").IsDependentOn("VerifyThatThereAreNoUncommittedChanges").IsDependentOn("VerifyThatDevelopmentBranchIsAheadOfMaster")
  .IsDependentOn("VerifyThatMasterBranchDoesNotHaveOpenPullRequests").IsDependentOn("VerifyThatDevelopmentBranchDoesNotHaveOpenPullRequests").IsDependentOn("VerifyThatPullRequestExistsForDevelopmentBranchHeadTip")
  .IsDependentOn("BuildAndTestDebugAndRelease").IsDependentOn("UpdateNuspec").IsDependentOn("CreateNuGetPackage")
  .Does(() => {
  });

Task("CleanRestorePullUpdateBuildCake")
  .Description("Clean, restore, pull, update build cake")
  .IsDependentOn("CleanRestorePull").IsDependentOn("UpdateBuildCake")
  .Does(() => {
  });

Task("LittleThings")
  .Description("Default but do not build or test in debug or release, and do not create or push nuget package")
  .IsDependentOn("CleanRestorePullUpdateBuildCake")
  .IsDependentOn("VerifyThatThereAreNoUncommittedChanges").IsDependentOn("VerifyThatDevelopmentBranchIsAheadOfMaster")
  .IsDependentOn("VerifyThatMasterBranchDoesNotHaveOpenPullRequests").IsDependentOn("VerifyThatDevelopmentBranchDoesNotHaveOpenPullRequests").IsDependentOn("VerifyThatPullRequestExistsForDevelopmentBranchHeadTip")
  .Does(() => {
  });

Task("ValidatePackageUpdate")
  .WithCriteria(() => currentGitBranch == "master")
  .Description("Build and test debug and release, update nuspec")
  .IsDependentOn("CleanRestorePull").IsDependentOn("VerifyThatThereAreUncommittedChanges")
  .IsDependentOn("BuildAndTestDebugAndRelease").IsDependentOn("UpdateNuspec")
  .Does(() => {
  });

Task("Default")
  .IsDependentOn("LittleThings").IsDependentOn("BuildAndTestDebugAndRelease")
  .IsDependentOn("UpdateNuspec").IsDependentOn("CreateNuGetPackage").IsDependentOn("PushNuGetPackage").IsDependentOn("CleanObjectFolders").Does(() => {
  });

RunTarget(target);