using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Extensions {
    [TestClass]
    public class WebHostBuilderExtensionsTest {
        private const string LocalSystemFolder = "TheLocalSystemFolder", MyFolder = "MyFolder";
        private const string NotExistingFile = "NotExistingFile", UnchangedFile = "UnchangedFile", ChangedFile = "ChangedFile";

        [TestMethod]
        public async Task CanUpdateSecrets() {
            var builderMock = new Mock<IWebHostBuilder>();
            var dvinRepositoryMock = new Mock<IDvinRepository>();
            var localSystemFolder = new LocalSystemFolder {
                SecretsFolder = LocalSystemFolder
            };
            dvinRepositoryMock.Setup(d => d.LoadFolderAsync()).Returns(Task.FromResult(localSystemFolder));
            dvinRepositoryMock.Setup(d => d.MySecretRepositoryFolder()).Returns(MyFolder);
            var fileSystemServiceMock = new Mock<IFileSystemService>();
            fileSystemServiceMock.Setup(f => f.ListFilesInDirectory(It.IsAny<IFolder>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns(
                new List<string> { LocalSystemFolder + NotExistingFile, LocalSystemFolder + UnchangedFile, LocalSystemFolder + ChangedFile }
            );
            fileSystemServiceMock.Setup(f => f.Exists(It.IsAny<string>())).Returns<string>(f => f != MyFolder + NotExistingFile);
            fileSystemServiceMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns<string>(f => {
                return f == LocalSystemFolder + ChangedFile ? "SomeChangedContent" : "SomeContent";
            });
            var copyActions = new Dictionary<string, string>();
            fileSystemServiceMock.Setup(f => f.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Callback<string, string, bool>((sf, df, o) => {
                copyActions[sf] = df;
            });
            await builderMock.Object.UpdateSecrets(dvinRepositoryMock.Object, fileSystemServiceMock.Object);
            Assert.AreEqual(2, copyActions.Count);
            Assert.IsTrue(copyActions.ContainsKey(LocalSystemFolder + NotExistingFile) && copyActions[LocalSystemFolder + NotExistingFile] == MyFolder + NotExistingFile);
            Assert.IsTrue(copyActions.ContainsKey(LocalSystemFolder + ChangedFile) && copyActions[LocalSystemFolder + ChangedFile] == MyFolder + ChangedFile);
        }
    }
}
