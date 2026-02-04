using System.IO;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test.Components;

[TestClass]
public class FileSystemServiceTest {
    [TestMethod]
    public void CanListFilesInDirectory() {
        var location = GetType().Assembly.Location;
        location = location.Substring(0, location.LastIndexOf('\\'));
        var folder = new Folder(location);
        var sut = new FileSystemService();
        var files = sut.ListFilesInDirectory(folder, "*.*", SearchOption.AllDirectories);
        Assert.Contains(location + @"\Autofac.dll", files);
    }
}